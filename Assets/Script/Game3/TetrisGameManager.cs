using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrateur du Jeu 3 (Tetris).
/// Joue le même rôle que Game1Manager et SnakeGameManager pour leurs jeux respectifs.
///
/// Responsabilités :
///   - Démarrer tous les systèmes au Start (timer, spawn, chute).
///   - Respawner la pièce suivante après chaque lock.
///   - Déclencher le game over (rechargement de Game3Scene) sur débordement ou spawn bloqué.
///   - Déclencher la victoire (transition EndScreen) quand TetrisTimerManager atteint 0.
///
/// Flux de respawn après lock :
///   TetrisPiece.Lock()
///     → AddBlock() → TetrisBoard.OnTopReached (si débordement)
///     → TetrisPiece.OnLocked émis
///       → TetrisLineManager supprime les lignes
///       → TetrisGameManager.HandlePieceLocked() → SpawnNextPiece()
///   TetrisFallController.OnPieceLocked émis (chute naturelle uniquement)
///
/// IMPORTANT : le respawn est déclenché depuis TetrisPiece.OnLocked (via OnPieceSpawned)
/// et NON depuis TetrisFallController.OnPieceLocked, car le hard drop (TetrisInputController)
/// appelle Lock() directement sans passer par FallController.
///
/// Responsabilité unique : orchestration. Aucune logique de gameplay direct.
/// </summary>
public class TetrisGameManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[TetrisGameManager]";

    // ── Références inspecteur — systèmes scène ────────────────────────────────

    [SerializeField, Tooltip("Spawner de pièces Tetris.")]
    private TetrisSpawner _spawner;

    [SerializeField, Tooltip("Contrôleur de chute automatique.")]
    private TetrisFallController _fallController;

    [SerializeField, Tooltip("Grille logique Tetris.")]
    private TetrisBoard _board;

    [SerializeField, Tooltip("Timer inversé local au Jeu 3.")]
    private TetrisTimerManager _timerManager;

    [SerializeField, Tooltip("Gestionnaire de lignes (validation au Start uniquement).")]
    private TetrisLineManager _lineManager;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Délai en secondes entre le game over et le rechargement de la scène.")]
    private float _delayBeforeReload = 1.5f;

    [SerializeField, Tooltip("Délai en secondes entre la victoire et la transition vers EndScreen.")]
    private float _delayBeforeTransition = 2f;

    [SerializeField, Tooltip("Si true, démarre GlobalTimerManager si ce n'est pas déjà fait. " +
                             "Utile pour les tests en isolation sans passer par GameFlowManager.")]
    private bool _autoStartGlobalTimerIfNotRunning = true;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Garde-fou : une seule transition de fin de jeu à la fois.</summary>
    private bool _gameEnded;

    // ── Event public ──────────────────────────────────────────────────────────

    /// <summary>
    /// Émis juste avant d'appeler GameFlowManager.AdvanceFlow() lors de la victoire.
    /// Par symétrie avec Game1Manager et SnakeGameManager.
    /// </summary>
    public event Action OnGame3Completed;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        // ── Timer global (persistant) ─────────────────────────────────────────
        if (_autoStartGlobalTimerIfNotRunning && GlobalTimerManager.Instance != null)
            GlobalTimerManager.Instance.StartTimer();

        // ── Timer inversé local au Jeu 3 ─────────────────────────────────────
        _timerManager.StartTimer();

        // ── Abonnements aux events de fin de partie ───────────────────────────
        _spawner.OnSpawnBlocked      += HandleSpawnBlocked;
        _board.OnTopReached          += HandleTopReached;
        _timerManager.OnTimerFinished += HandleTimerFinished;

        // ── Abonnement au respawn inter-pièces ────────────────────────────────
        // On s'abonne à OnPieceSpawned pour obtenir la référence de chaque pièce
        // fraîchement instanciée et s'abonner à son OnLocked.
        // Cela couvre les deux origines de lock : FallController ET InputController (hard drop).
        _spawner.OnPieceSpawned += HandlePieceSpawned;

        // ── Démarrage ─────────────────────────────────────────────────────────
        bool ok = _spawner.SpawnNextPiece();

        if (!ok)
        {
            // Cas pathologique : la grille est pleine avant même de commencer.
            Debug.LogError($"{LOG_PREFIX} Spawn initial impossible — état de grille invalide.");
            return;
        }

        _fallController.StartFalling();

        Debug.Log($"{LOG_PREFIX} Démarré. Timer inversé : {_timerManager.GetFormattedTime()}. " +
                  "En attente de game over ou victoire.");
    }

    private void OnDestroy()
    {
        if (_spawner != null)
        {
            _spawner.OnSpawnBlocked -= HandleSpawnBlocked;
            _spawner.OnPieceSpawned -= HandlePieceSpawned;
        }

        if (_board != null)
            _board.OnTopReached -= HandleTopReached;

        if (_timerManager != null)
            _timerManager.OnTimerFinished -= HandleTimerFinished;
    }

    // ── Handler : respawn inter-pièces ────────────────────────────────────────

    /// <summary>
    /// Appelé par TetrisSpawner.OnPieceSpawned à chaque spawn.
    /// S'abonne au OnLocked de la nouvelle pièce (abonnement one-shot).
    /// Ce handler couvre les deux origines de lock : chute naturelle ET hard drop.
    /// </summary>
    private void HandlePieceSpawned(PieceType type)
    {
        TetrisPiece piece = _spawner.CurrentPiece;

        if (piece == null) return;

        System.Action<Vector2Int[]> onLockedHandler = null;
        onLockedHandler = _ =>
        {
            piece.OnLocked -= onLockedHandler;
            HandlePieceLocked();
        };

        piece.OnLocked += onLockedHandler;
    }

    /// <summary>
    /// Appelé quand la pièce active est verrouillée (par FallController ou InputController).
    /// À ce stade, TetrisLineManager a déjà supprimé les lignes complètes —
    /// TetrisPiece.OnLocked est émis avant TetrisFallController.OnPieceLocked,
    /// et TetrisLineManager s'abonne également à TetrisPiece.OnLocked.
    /// L'ordre entre LineManager et GameManager sur le même event n'est pas garanti,
    /// mais le respawn ici n'empêche pas le line clear car il opère sur la grille,
    /// non sur la pièce déjà lockée.
    /// </summary>
    private void HandlePieceLocked()
    {
        if (_gameEnded) return;

        // Si un bloc a atteint le sommet, OnTopReached a déjà été émis par AddBlock()
        // pendant Lock(). HandleTopReached() va setter _gameEnded — on vérifie.
        if (_board.HasBlockAboveTopLine()) return;

        _spawner.SpawnNextPiece();
        // Si le spawn échoue, OnSpawnBlocked est émis et géré par HandleSpawnBlocked.
    }

    // ── Handlers : fin de jeu ─────────────────────────────────────────────────

    /// <summary>
    /// Game over : le spawn de la nouvelle pièce est impossible (grille trop haute).
    /// </summary>
    private void HandleSpawnBlocked()
    {
        if (_gameEnded) return;

        _gameEnded = true;

        Debug.Log($"{LOG_PREFIX} Game over (raison : SpawnBlocked). " +
                  $"Rechargement dans {_delayBeforeReload}s.");

        StopGameSystems();
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.TetrisGameOver);
        StartCoroutine(ReloadGame3Routine());
    }

    /// <summary>
    /// Game over : un bloc a atteint la ligne du haut de la grille.
    /// Émis par TetrisBoard.AddBlock() pendant TetrisPiece.Lock().
    /// </summary>
    private void HandleTopReached()
    {
        if (_gameEnded) return;

        _gameEnded = true;

        Debug.Log($"{LOG_PREFIX} Game over (raison : TopReached). " +
                  $"Rechargement dans {_delayBeforeReload}s.");

        StopGameSystems();
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.TetrisGameOver);
        StartCoroutine(ReloadGame3Routine());
    }

    /// <summary>
    /// Victoire : le timer inversé a atteint 0.
    /// </summary>
    private void HandleTimerFinished()
    {
        if (_gameEnded) return;

        _gameEnded = true;

        Debug.Log($"{LOG_PREFIX} Victoire ! Timer inversé atteint 0. " +
                  $"Transition dans {_delayBeforeTransition}s.");

        StopGameSystems();
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.TetrisWin);
        StartCoroutine(TransitionToEndRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// Attend _delayBeforeReload secondes puis recharge Game3Scene.
    /// Le timer global continue de tourner — c'est la pénalité pour le game over.
    /// </summary>
    private IEnumerator ReloadGame3Routine()
    {
        yield return new WaitForSeconds(_delayBeforeReload);

        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.Game3);
    }

    /// <summary>
    /// Attend _delayBeforeTransition secondes, stoppe le timer global,
    /// émet OnGame3Completed et avance le flux vers EndScreen.
    /// </summary>
    private IEnumerator TransitionToEndRoutine()
    {
        yield return new WaitForSeconds(_delayBeforeTransition);

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.GameTransition);

        // Figer le temps final dans SO_GameSession avant la transition.
        GlobalTimerManager.Instance?.StopTimer();

        OnGame3Completed?.Invoke();

        GameFlowManager.Instance?.AdvanceFlow();
    }

    // ── Utilitaires privés ────────────────────────────────────────────────────

    /// <summary>Arrête les systèmes de jeu (chute et timer inversé).</summary>
    private void StopGameSystems()
    {
        _fallController.StopFalling();
        _timerManager.StopTimer();
    }

    /// <summary>
    /// Valide toutes les références sérialisées au Start.
    /// Retourne false si une référence critique est manquante.
    /// </summary>
    private bool ValidateReferences()
    {
        bool valid = true;

        if (_spawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _spawner non assigné dans l'inspecteur.", this);
            valid = false;
        }

        if (_fallController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _fallController non assigné dans l'inspecteur.", this);
            valid = false;
        }

        if (_board == null)
        {
            Debug.LogError($"{LOG_PREFIX} _board non assigné dans l'inspecteur.", this);
            valid = false;
        }

        if (_timerManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} _timerManager non assigné dans l'inspecteur.", this);
            valid = false;
        }

        if (_lineManager == null)
            Debug.LogWarning($"{LOG_PREFIX} _lineManager non assigné — vérifier le câblage de la scène.", this);

        return valid;
    }
}
