using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gère le déplacement de la tête du chevalier (Snake) sur la grille logique.
/// 
/// - Le chevalier démarre IMMOBILE au centre de la grille.
/// - Au premier swipe, il commence à avancer dans la direction swaipée.
/// - Il avance d'une case toutes les <see cref="_moveInterval"/> secondes.
/// - Les demi-tours instantanés sont interdits.
/// - Émet des events à chaque mouvement pour que SnakeBody et SnakeGameManager réagissent.
/// 
/// Responsabilité unique : déplacer la tête et émettre des events.
/// Aucune logique de queue, de collision ou de game over.
/// À placer sur le GameObject "Knight" dans Game2Scene.
/// </summary>
public class SnakeMovementController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SnakeMovement]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Grille logique du Snake. Fournit les conversions et l'état des cases.")]
    private SnakeGrid _grid;

    // ── Paramètres — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Durée en secondes entre chaque déplacement d'une case. " +
                             "0.2s = 5 cases/seconde.")]
    private float _moveInterval = 0.2f;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>Émis une seule fois au premier swipe, quand le chevalier commence à bouger.</summary>
    public event Action OnFirstMoveStarted;

    /// <summary>
    /// Émis juste AVANT chaque déplacement.
    /// Paramètres : ancienne position logique, nouvelle position logique.
    /// Permet à SnakeBody de réagir avant la mise à jour de la grille.
    /// </summary>
    public event Action<Vector2Int, Vector2Int> OnAboutToMove;

    /// <summary>
    /// Émis juste APRÈS chaque déplacement.
    /// Paramètre : nouvelle position logique du chevalier.
    /// </summary>
    public event Action<Vector2Int> OnMoved;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Position actuelle du chevalier en coordonnées logiques.</summary>
    public Vector2Int CurrentGridPosition => _currentGridPosition;

    /// <summary>Direction de déplacement courante. Vector2Int.zero si immobile.</summary>
    public Vector2Int CurrentDirection => _currentDirection;

    /// <summary>Vrai si le chevalier est en mouvement (après le premier swipe).</summary>
    public bool IsMoving => _isMoving;

    // ── État interne ──────────────────────────────────────────────────────────

    private Vector2Int _currentGridPosition;
    private Vector2Int _currentDirection = Vector2Int.zero;
    private bool       _isMoving;
    private Coroutine  _moveCoroutine;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        PlaceAtCenter();
        SubscribeToSwipeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSwipeEvents();
        StopMoveCoroutine();
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Téléporte le chevalier au centre de la grille et marque la case.
    /// </summary>
    private void PlaceAtCenter()
    {
        _currentGridPosition = _grid.Center;
        transform.position   = _grid.GridToWorld(_currentGridPosition);
        _grid.SetCell(_currentGridPosition, CellState.Knight);

        Debug.Log($"{LOG_PREFIX} Chevalier placé en {_currentGridPosition} " +
                  $"(monde : {transform.position}).");
    }

    /// <summary>
    /// S'abonne aux quatre events de swipe de SwipeInputReader.
    /// Guard-fou si la scène est lancée en isolation sans _PersistentManagers.
    /// </summary>
    private void SubscribeToSwipeEvents()
    {
        if (SwipeInputReader.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} SwipeInputReader.Instance est null. " +
                           "Lance le jeu depuis MainMenuScene pour le flux complet, " +
                           "ou assure-toi que _PersistentManagers est dans la scène.");
            return;
        }

        SwipeInputReader.Instance.OnSwipeLeft  += HandleSwipeLeft;
        SwipeInputReader.Instance.OnSwipeRight += HandleSwipeRight;
        SwipeInputReader.Instance.OnSwipeUp    += HandleSwipeUp;
        SwipeInputReader.Instance.OnSwipeDown  += HandleSwipeDown;
    }

    /// <summary>
    /// Se désabonne des events de swipe. Vérifie que l'Instance existe encore
    /// (peut être null si _PersistentManagers a été détruit en premier).
    /// </summary>
    private void UnsubscribeFromSwipeEvents()
    {
        if (SwipeInputReader.Instance == null) return;

        SwipeInputReader.Instance.OnSwipeLeft  -= HandleSwipeLeft;
        SwipeInputReader.Instance.OnSwipeRight -= HandleSwipeRight;
        SwipeInputReader.Instance.OnSwipeUp    -= HandleSwipeUp;
        SwipeInputReader.Instance.OnSwipeDown  -= HandleSwipeDown;
    }

    // ── Handlers de swipe ─────────────────────────────────────────────────────

    private void HandleSwipeLeft()  => TryChangeDirection(Vector2Int.left);
    private void HandleSwipeRight() => TryChangeDirection(Vector2Int.right);
    private void HandleSwipeUp()    => TryChangeDirection(Vector2Int.up);
    private void HandleSwipeDown()  => TryChangeDirection(Vector2Int.down);

    // ── Logique de direction ──────────────────────────────────────────────────

    /// <summary>
    /// Tente de changer la direction du chevalier.
    /// - Premier swipe : démarre le mouvement.
    /// - Swipes suivants : tourne si la direction n'est pas un demi-tour.
    /// </summary>
    /// <param name="newDirection">Direction demandée par le joueur.</param>
    private void TryChangeDirection(Vector2Int newDirection)
    {
        if (!_isMoving)
        {
            // Premier swipe : démarrage du mouvement
            _currentDirection = newDirection;
            _isMoving         = true;
            _moveCoroutine    = StartCoroutine(MoveRoutine());

            OnFirstMoveStarted?.Invoke();
            return;
        }

        // Interdit le demi-tour instantané (ex : droite → gauche)
        if (newDirection == -_currentDirection)
            return;

        // Le changement de direction prend effet à la prochaine itération de MoveRoutine
        _currentDirection = newDirection;
    }

    // ── Coroutine de déplacement ──────────────────────────────────────────────

    /// <summary>
    /// Boucle principale du déplacement.
    /// Avance d'une case toutes les <see cref="_moveInterval"/> secondes.
    /// Continue jusqu'à ce que StopMovement() soit appelé.
    /// </summary>
    private IEnumerator MoveRoutine()
    {
        while (_isMoving)
        {
            yield return new WaitForSeconds(_moveInterval);

            Vector2Int oldPos = _currentGridPosition;
            Vector2Int newPos = _currentGridPosition + _currentDirection;

            // Notifier AVANT la mise à jour de la grille.
            // MODIFIÉ — Option A : SnakeBody est seul responsable de libérer oldPos.
            // Si la queue est vide, SnakeBody la met à Empty.
            // Si la queue est présente, SnakeBody la marque Tail (segment 0).
            // Ce script ne touche plus à oldPos après l'émission de l'event.
            OnAboutToMove?.Invoke(oldPos, newPos);

            // Marquer la nouvelle case si dans la grille (si hors grille → le GameManager gère)
            if (_grid.IsInsideGrid(newPos))
                _grid.SetCell(newPos, CellState.Knight);

            // Déplacer le GameObject dans le monde
            transform.position   = _grid.GridToWorld(newPos);
            _currentGridPosition = newPos;

            OnMoved?.Invoke(newPos);
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Arrête immédiatement le déplacement du chevalier.
    /// Appelé par SnakeGameManager lors d'un game over ou d'une victoire.
    /// </summary>
    public void StopMovement()
    {
        _isMoving = false;
        StopMoveCoroutine();
    }

    // ── Méthodes privées utilitaires ──────────────────────────────────────────

    private void StopMoveCoroutine()
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }
    }

    /// <summary>Vérifie les dépendances obligatoires. Retourne false si l'une manque.</summary>
    private bool ValidateReferences()
    {
        if (_grid != null) return true;

        Debug.LogError($"{LOG_PREFIX} _grid non assigné dans l'inspecteur. " +
                       "Glisse le GameObject SnakeGrid dans le champ _grid.");
        return false;
    }
}
