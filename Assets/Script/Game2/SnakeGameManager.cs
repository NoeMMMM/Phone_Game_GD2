using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrateur du Jeu 2 (Snake).
/// Responsabilité unique : démarrer les systèmes au lancement de la scène
/// et déclencher les transitions appropriées selon l'issue de la partie.
///
/// - Game over (WallHit ou SelfCollision) → arrêt du mouvement + rechargement de Game2Scene
///   après un délai. Le timer global continue de tourner.
/// - Victoire (grille pleine) → arrêt du mouvement + transition vers Lore3_Story
///   via GameFlowManager.AdvanceFlow().
///
/// Ne contient aucune logique de gameplay (grille, segments, indices).
/// À placer sur un GameObject "SnakeGameManager" dans Game2Scene.
/// </summary>
public class SnakeGameManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SnakeGameManager]";

    // ── Références — inspecteur (scripts de la scène) ─────────────────────────

    [SerializeField, Tooltip("Détecteur de collisions de la scène — source des events OnGameOver et OnVictory.")]
    private SnakeCollisionHandler _collisionHandler;

    [SerializeField, Tooltip("Contrôleur de déplacement du chevalier — appelé pour stopper le mouvement.")]
    private SnakeMovementController _movementController;

    [SerializeField, Tooltip("Gestionnaire d'indices — référence gardée pour usage futur (nettoyage, etc.).")]
    private ClueSpawner _clueSpawner;

    // ── Paramètres — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Délai en secondes entre le game over et le rechargement de Game2Scene.")]
    private float _delayBeforeReload = 1.5f;

    [SerializeField, Tooltip("Délai en secondes entre la victoire et la transition vers Lore3_Story.")]
    private float _delayBeforeTransition = 2f;

    [SerializeField, Tooltip("Si true, démarre le timer global au Start() s'il n'est pas déjà en cours. " +
                             "Utile pour tester Game2Scene en isolation sans passer par le menu.")]
    private bool _autoStartTimerIfNotRunning = true;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Garde-fou : empêche de déclencher plusieurs transitions simultanées.</summary>
    private bool _gameEnded;

    // ── Event public ──────────────────────────────────────────────────────────

    /// <summary>
    /// Émis une seule fois quand le Jeu 2 est terminé avec succès, après le délai de transition.
    /// Symétrique à Game1Manager.OnGame1Completed.
    /// </summary>
    public event Action OnGame2Completed;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateDependencies()) return;

        TryStartTimer();

        _collisionHandler.OnGameOver += HandleGameOver;
        _collisionHandler.OnVictory  += HandleVictory;

        Debug.Log($"{LOG_PREFIX} Démarré. En attente de game over ou victoire.");
    }

    private void OnDestroy()
    {
        if (_collisionHandler == null) return;

        _collisionHandler.OnGameOver -= HandleGameOver;
        _collisionHandler.OnVictory  -= HandleVictory;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par SnakeCollisionHandler.OnGameOver.
    /// Arrête le mouvement et lance le rechargement de la scène après délai.
    /// </summary>
    private void HandleGameOver(GameOverReason reason)
    {
        if (_gameEnded) return;

        _gameEnded = true;

        Debug.Log($"{LOG_PREFIX} Game over (raison : {reason}). " +
                  $"Rechargement dans {_delayBeforeReload}s.");

        _movementController.StopMovement();

        StartCoroutine(ReloadGame2Routine());
    }

    /// <summary>
    /// Appelé par SnakeCollisionHandler.OnVictory.
    /// Arrête le mouvement et lance la transition vers le prochain écran après délai.
    /// </summary>
    private void HandleVictory()
    {
        if (_gameEnded) return;

        _gameEnded = true;

        Debug.Log($"{LOG_PREFIX} Victoire ! Transition dans {_delayBeforeTransition}s.");

        _movementController.StopMovement();

        StartCoroutine(TransitionToNextRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// Attend le délai configuré puis recharge Game2Scene.
    /// Le timer global continue de tourner (pénalité de temps pour l'échec).
    /// </summary>
    private IEnumerator ReloadGame2Routine()
    {
        yield return new WaitForSeconds(_delayBeforeReload);

        if (SceneLoader.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} SceneLoader.Instance est null — impossible de recharger la scène. " +
                           "Lance le jeu depuis MainMenuScene pour le flux complet.");
            yield break;
        }

        SceneLoader.Instance.LoadScene(SceneLoader.SceneName.Game2);
    }

    /// <summary>
    /// Attend le délai configuré, joue le feedback de transition,
    /// émet OnGame2Completed, puis délègue à GameFlowManager.AdvanceFlow().
    /// </summary>
    private IEnumerator TransitionToNextRoutine()
    {
        yield return new WaitForSeconds(_delayBeforeTransition);

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.GameTransition);

        // Émission avant AdvanceFlow pour que les abonnés locaux réagissent avant le changement de scène.
        OnGame2Completed?.Invoke();

        if (GameFlowManager.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} GameFlowManager.Instance est null — impossible d'avancer dans le flux. " +
                           "Lance le jeu depuis MainMenuScene pour le flux complet.");
            yield break;
        }

        GameFlowManager.Instance.AdvanceFlow();
    }

    // ── Méthodes privées utilitaires ──────────────────────────────────────────

    /// <summary>
    /// Démarre le timer global s'il n'est pas déjà en cours.
    /// Dans le flux normal, GameFlowManager l'a déjà démarré à l'entrée du Jeu 1.
    /// Cette vérification ne sert qu'aux tests en isolation de Game2Scene.
    /// </summary>
    private void TryStartTimer()
    {
        if (!_autoStartTimerIfNotRunning) return;

        if (GlobalTimerManager.Instance == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} GlobalTimerManager.Instance est null — " +
                             "timer non démarré. Lance depuis MainMenuScene pour le flux complet.");
            return;
        }

        // GetCurrentTime() == 0 indique que la session n'a pas encore commencé
        // (le timer est déjà actif dans le flux normal).
        if (GlobalTimerManager.Instance.GetCurrentTime() == 0f)
        {
            GlobalTimerManager.Instance.StartTimer();
            Debug.Log($"{LOG_PREFIX} Timer global démarré par SnakeGameManager " +
                      "(_autoStartTimerIfNotRunning = true).");
        }
    }

    /// <summary>Vérifie les dépendances obligatoires. Retourne false si l'une manque.</summary>
    private bool ValidateDependencies()
    {
        bool isValid = true;

        if (_collisionHandler == null)
        {
            Debug.LogError($"{LOG_PREFIX} _collisionHandler non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_movementController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _movementController non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_clueSpawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _clueSpawner non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        return isValid;
    }
}
