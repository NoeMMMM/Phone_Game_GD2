using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gère la chute automatique de la pièce active et son verrouillage quand
/// elle ne peut plus descendre.
///
/// Flux :
///   1. TetrisGameManager appelle StartFalling() au début de la partie.
///   2. FallRoutine() tourne en boucle : attend _currentFallInterval secondes,
///      puis tente TryDropOne() sur la pièce courante.
///   3. Si TryDropOne() retourne false, la pièce est lockée et OnPieceLocked est émis.
///      TetrisGameManager (ou un autre script) écoute cet event pour déclencher
///      SpawnNextPiece() — le respawn n'est PAS fait ici.
///   4. L'intervalle de chute diminue toutes les _accelerationInterval secondes
///      (multiplicateur _accelerationFactor), jusqu'à _minFallInterval.
///
/// Responsabilité unique : chute automatique, accélération progressive, lock sur blocage.
/// Aucune gestion d'input, aucune détection de lignes, aucun respawn.
/// </summary>
public class TetrisFallController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[FallController]";

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Spawner Tetris — pour accéder à CurrentPiece et écouter OnPieceSpawned.")]
    private TetrisSpawner _spawner;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Intervalle de chute initial en secondes (1 case / sec par défaut).")]
    private float _initialFallInterval = 1f;

    [SerializeField, Tooltip("Toutes les X secondes écoulées, l'intervalle de chute est multiplié par _accelerationFactor.")]
    private float _accelerationInterval = 30f;

    [SerializeField, Tooltip("Multiplicateur appliqué à chaque palier d'accélération (0.9 = -10%).")]
    private float _accelerationFactor = 0.9f;

    [SerializeField, Tooltip("Intervalle de chute minimum en secondes (vitesse plafond).")]
    private float _minFallInterval = 0.05f;

    // ── État interne ──────────────────────────────────────────────────────────

    private float _currentFallInterval;
    private float _timeSinceLastAcceleration;
    private Coroutine _fallCoroutine;
    private bool _isActive;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis quand la pièce courante est lockée automatiquement (impossible de descendre).
    /// TetrisGameManager écoute cet event pour spawner la pièce suivante.
    /// </summary>
    public event Action OnPieceLocked;

    /// <summary>
    /// Émis à chaque palier d'accélération.
    /// Paramètre : nouvel intervalle de chute en secondes (pour HUD futur).
    /// </summary>
    public event Action<float> OnSpeedChanged;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Intervalle de chute actuel en secondes.</summary>
    public float CurrentFallInterval => _currentFallInterval;

    /// <summary>True si la coroutine de chute tourne.</summary>
    public bool IsActive => _isActive;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_spawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _spawner non assigné dans l'inspecteur.", this);
            return;
        }

        _currentFallInterval = _initialFallInterval;
        _spawner.OnPieceSpawned += HandlePieceSpawned;
    }

    private void OnDestroy()
    {
        if (_spawner != null)
            _spawner.OnPieceSpawned -= HandlePieceSpawned;

        StopFalling();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Démarre la coroutine de chute automatique.
    /// Sans effet si déjà active.
    /// </summary>
    public void StartFalling()
    {
        _isActive = true;

        if (_fallCoroutine == null)
            _fallCoroutine = StartCoroutine(FallRoutine());
    }

    /// <summary>
    /// Arrête la coroutine de chute automatique.
    /// Sans effet si déjà arrêtée.
    /// </summary>
    public void StopFalling()
    {
        _isActive = false;

        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }
    }

    /// <summary>
    /// Remet l'intervalle de chute à sa valeur initiale et réinitialise le timer d'accélération.
    /// Appelé par TetrisGameManager après un game over (avant de redémarrer la partie).
    /// </summary>
    public void ResetFallSpeed()
    {
        _currentFallInterval          = _initialFallInterval;
        _timeSinceLastAcceleration    = 0f;
    }

    // ── Handlers privés ───────────────────────────────────────────────────────

    /// <summary>
    /// Appelé quand TetrisSpawner émet OnPieceSpawned.
    /// La coroutine est déjà en cours et prendra en charge la nouvelle pièce
    /// au prochain tick — pas besoin de la redémarrer.
    /// </summary>
    private void HandlePieceSpawned(PieceType type)
    {
        Debug.Log($"{LOG_PREFIX} Nouvelle pièce reçue ({type}), chute reprise.");
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Boucle principale de chute automatique.
    /// Attend _currentFallInterval secondes entre chaque tentative de descente.
    /// Gère l'accélération progressive et le lock automatique.
    /// </summary>
    private IEnumerator FallRoutine()
    {
        while (_isActive)
        {
            yield return new WaitForSeconds(_currentFallInterval);

            // ── Accélération progressive ──────────────────────────────────────
            _timeSinceLastAcceleration += _currentFallInterval;

            if (_timeSinceLastAcceleration >= _accelerationInterval)
            {
                _currentFallInterval       = Mathf.Max(_currentFallInterval * _accelerationFactor, _minFallInterval);
                _timeSinceLastAcceleration = 0f;

                OnSpeedChanged?.Invoke(_currentFallInterval);
                Debug.Log($"{LOG_PREFIX} Accélération ! Nouvel intervalle : {_currentFallInterval:F3}s");
            }

            // ── Chute ─────────────────────────────────────────────────────────
            if (_spawner.CurrentPiece == null)
                continue; // Aucune pièce active, on attend le prochain spawn.

            bool dropped = _spawner.CurrentPiece.TryDropOne();

            if (!dropped)
            {
                // La pièce ne peut plus descendre : on la lock et on signale.
                _spawner.CurrentPiece.Lock();
                OnPieceLocked?.Invoke();
            }
        }

        _fallCoroutine = null;
    }
}
