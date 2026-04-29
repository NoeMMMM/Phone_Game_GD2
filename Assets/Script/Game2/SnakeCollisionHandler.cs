using System;
using UnityEngine;

// ── Enum public — accessible depuis SnakeGameManager et SnakeCollisionDebugger ──

/// <summary>
/// Raison d'un game over dans le Jeu 2 (Snake).
/// </summary>
public enum GameOverReason
{
    /// <summary>La tête est sortie des limites de la grille.</summary>
    WallHit,

    /// <summary>La tête est entrée en collision avec un segment de sa propre queue.</summary>
    SelfCollision
}

/// <summary>
/// Détecte les événements de jeu du Snake après chaque déplacement de la tête.
///
/// S'abonne à SnakeMovementController.OnMoved (émis APRÈS la mise à jour de la grille).
/// À ce moment, la grille reflète la nouvelle position de la tête (Knight) et la queue
/// a déjà glissé (SnakeBody a réagi à OnAboutToMove). Ce script est donc le dernier
/// à réagir dans la chaîne de mouvement.
///
/// Ordre de vérification (priorité décroissante) :
///   1. Sortie de grille   → game over (WallHit)
///   2. Collision avec queue → game over (SelfCollision)
///   3. Ramassage d'indice  → AddSegment + RespawnClue (ou victoire si grille pleine)
///
/// Note : la détection de queue et d'indice se fait via les APIs de SnakeBody et
/// ClueSpawner, PAS via l'état de la grille — plus robuste face aux écrasements de
/// SetCell qui se produisent dans MoveRoutine.
///
/// Responsabilité unique : détecter et émettre. Aucune transition de scène,
/// aucun appel à GameFlowManager. SnakeGameManager écoute les events de ce script.
/// À placer sur un GameObject "SnakeCollisionHandler" dans Game2Scene.
/// </summary>
public class SnakeCollisionHandler : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SnakeCollision]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("SnakeMovementController du GameObject Knight. " +
                             "Fournit OnMoved.")]
    private SnakeMovementController _movementController;

    [SerializeField, Tooltip("Grille logique du Snake. Fournit IsInsideGrid et IsGridFull.")]
    private SnakeGrid _grid;

    [SerializeField, Tooltip("Gestionnaire de queue. Fournit ContainsPosition et AddSegment.")]
    private SnakeBody _body;

    [SerializeField, Tooltip("Gestionnaire d'indices. Fournit CurrentCluePosition, " +
                             "HasActiveClue et RespawnClue.")]
    private ClueSpawner _clueSpawner;

    [SerializeField, Tooltip("Nombre de segments ajoutés à la queue à chaque ramassage d'indice.")]
    private int _segmentsPerClue = 3;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>Émis quand la tête sort des limites de la grille.</summary>
    public event Action OnWallHit;

    /// <summary>Émis quand la tête entre en collision avec un segment de queue.</summary>
    public event Action OnSelfCollision;

    /// <summary>
    /// Émis quand la tête ramasse un indice.
    /// Paramètre : position logique de l'indice ramassé.
    /// </summary>
    public event Action<Vector2Int> OnClueCollected;

    /// <summary>
    /// Émis lors d'un game over, quelle qu'en soit la raison.
    /// Paramètre : raison du game over.
    /// </summary>
    public event Action<GameOverReason> OnGameOver;

    /// <summary>Émis quand la grille est entièrement remplie après un ramassage.</summary>
    public event Action OnVictory;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>True si un game over a déjà été déclenché ce cycle de jeu.</summary>
    public bool IsGameOver => _gameOverTriggered;

    /// <summary>True si la victoire a déjà été déclenchée ce cycle de jeu.</summary>
    public bool IsVictory => _victoryTriggered;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>
    /// Garde-fou : une fois à true, HandleMoved ne traite plus aucun mouvement.
    /// Évite les doubles émissions d'OnGameOver si le chevalier continue à bouger.
    /// </summary>
    private bool _gameOverTriggered;

    /// <summary>Idem pour la victoire.</summary>
    private bool _victoryTriggered;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        _movementController.OnMoved += HandleMoved;
    }

    private void OnDestroy()
    {
        if (_movementController != null)
            _movementController.OnMoved -= HandleMoved;
    }

    // ── Handler principal ─────────────────────────────────────────────────────

    /// <summary>
    /// Appelé après chaque déplacement de la tête.
    /// Vérifie les collisions dans l'ordre de priorité décroissante.
    /// </summary>
    /// <param name="newPos">Nouvelle position logique de la tête.</param>
    private void HandleMoved(Vector2Int newPos)
    {
        // Garde-fou global : on n'agit plus après un game over ou une victoire.
        if (_gameOverTriggered || _victoryTriggered) return;

        // ── 1. Sortie de grille ───────────────────────────────────────────────
        if (!_grid.IsInsideGrid(newPos))
        {
            _gameOverTriggered = true;

            OnWallHit?.Invoke();
            OnGameOver?.Invoke(GameOverReason.WallHit);

            FeedbackManager.Instance?.PlayFeedback(FeedbackType.SnakeDeath);

            Debug.Log($"{LOG_PREFIX} Sortie de grille en {newPos}. GAME OVER.");
            return;
        }

        // ── 2. Collision avec la queue ────────────────────────────────────────
        // On demande à SnakeBody plutôt qu'à la grille : SnakeMovementController a
        // déjà écrasé la case en Knight via SetCell, rendant l'info Tail indisponible.
        if (_body.ContainsPosition(newPos))
        {
            _gameOverTriggered = true;

            OnSelfCollision?.Invoke();
            OnGameOver?.Invoke(GameOverReason.SelfCollision);

            FeedbackManager.Instance?.PlayFeedback(FeedbackType.SnakeDeath);

            Debug.Log($"{LOG_PREFIX} Collision avec la queue en {newPos}. GAME OVER.");
            return;
        }

        // ── 3. Ramassage d'indice ─────────────────────────────────────────────
        // On compare la position avec ClueSpawner plutôt qu'avec la grille :
        // SetCell(newPos, Knight) a écrasé le CellState.Clue avant OnMoved.
        if (_clueSpawner.HasActiveClue && _clueSpawner.CurrentCluePosition == newPos)
        {
            OnClueCollected?.Invoke(newPos);
            FeedbackManager.Instance?.PlayFeedback(FeedbackType.SnakeEat);

            _body.AddSegments(_segmentsPerClue);

            Debug.Log($"{LOG_PREFIX} Indice ramassé en {newPos}. Longueur queue : {_body.Length}.");

            // Vérifier la victoire APRÈS l'ajout du segment (la grille peut être pleine).
            if (_grid.IsGridFull())
            {
                _victoryTriggered = true;

                OnVictory?.Invoke();
                FeedbackManager.Instance?.PlayFeedback(FeedbackType.SnakeWin);

                Debug.Log($"{LOG_PREFIX} Grille pleine. VICTOIRE !");
                return;
            }

            // Pas de victoire : faire apparaître le prochain indice.
            _clueSpawner.RespawnClue();
        }
    }

    // ── Méthodes privées utilitaires ──────────────────────────────────────────

    /// <summary>Vérifie les dépendances obligatoires. Retourne false si l'une manque.</summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_movementController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _movementController non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_grid == null)
        {
            Debug.LogError($"{LOG_PREFIX} _grid non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_body == null)
        {
            Debug.LogError($"{LOG_PREFIX} _body non assigné dans l'inspecteur.", this);
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
