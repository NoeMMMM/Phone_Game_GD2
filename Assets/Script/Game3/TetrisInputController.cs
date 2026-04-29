using System.Collections;
using UnityEngine;

/// <summary>
/// Pont entre SwipeInputReader et la pièce Tetris active.
///
/// Mapping des contrôles :
///   Tap simple     → TryRotate() (rotation horaire 90°)
///   Swipe bas      → HardDrop() + Lock() immédiat (action terminale volontaire)
///   Hold gauche    → TryMove(left) en DAS (Delayed Auto Shift)
///   Hold droite    → TryMove(right) en DAS
///
/// DAS : premier mouvement immédiat, puis délai _holdInitialDelay, puis répétition
/// à intervalle _holdRepeatInterval tant que le doigt est posé.
///
/// Responsabilité unique : écouter SwipeInputReader et transmettre à TetrisPiece.
/// Aucune logique de chute, aucun spawn, aucune détection de lignes.
/// </summary>
public class TetrisInputController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[InputController]";

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Spawner Tetris — pour accéder à CurrentPiece.")]
    private TetrisSpawner _spawner;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Intervalle en secondes entre deux mouvements répétés pendant un hold.")]
    private float _holdRepeatInterval = 0.1f;

    [SerializeField, Tooltip("Délai initial en secondes avant le premier mouvement répété (DAS). " +
                             "Donne le temps de distinguer un tap d'un hold.")]
    private float _holdInitialDelay = 0.15f;

    // ── État interne ──────────────────────────────────────────────────────────

    private bool        _isHolding;
    private Vector2Int  _holdDirection;
    private Coroutine   _holdRepeatCoroutine;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_spawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _spawner non assigné dans l'inspecteur.", this);
            return;
        }

        if (SwipeInputReader.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} SwipeInputReader.Instance est null. " +
                           "Vérifier que le singleton persistent est présent dans la scène.", this);
            return;
        }

        SwipeInputReader.Instance.OnTap       += HandleTap;
        SwipeInputReader.Instance.OnSwipeDown += HandleSwipeDown;
        SwipeInputReader.Instance.OnHoldStart += HandleHoldStart;
        SwipeInputReader.Instance.OnHoldEnd   += HandleHoldEnd;
    }

    private void OnDestroy()
    {
        StopHold();

        if (SwipeInputReader.Instance == null) return;

        SwipeInputReader.Instance.OnTap       -= HandleTap;
        SwipeInputReader.Instance.OnSwipeDown -= HandleSwipeDown;
        SwipeInputReader.Instance.OnHoldStart -= HandleHoldStart;
        SwipeInputReader.Instance.OnHoldEnd   -= HandleHoldEnd;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    /// <summary>Tap simple → rotation horaire de la pièce active.</summary>
    private void HandleTap(Vector2 screenPosition)
    {
        if (_spawner.CurrentPiece == null) return;

        _spawner.CurrentPiece.TryRotate();
    }

    /// <summary>
    /// Swipe bas → hard drop immédiat + lock.
    /// Le lock est volontaire ici (action explicite du joueur) — on ne laisse pas
    /// FallController le détecter au prochain tick, ce qui introduirait jusqu'à
    /// _initialFallInterval secondes de latence perceptible.
    /// </summary>
    private void HandleSwipeDown()
    {
        if (_spawner.CurrentPiece == null) return;

        _spawner.CurrentPiece.HardDrop();
        _spawner.CurrentPiece.Lock();
    }

    /// <summary>
    /// Début de hold → détermine la direction (moitié gauche ou droite de l'écran)
    /// et démarre la coroutine DAS.
    /// </summary>
    private void HandleHoldStart(Vector2 screenPosition)
    {
        if (_spawner.CurrentPiece == null) return;

        _holdDirection = screenPosition.x < Screen.width * 0.5f
            ? Vector2Int.left
            : Vector2Int.right;

        _isHolding = true;

        if (_holdRepeatCoroutine != null)
            StopCoroutine(_holdRepeatCoroutine);

        _holdRepeatCoroutine = StartCoroutine(HoldRepeatRoutine());
    }

    /// <summary>Fin de hold → arrête la répétition latérale.</summary>
    private void HandleHoldEnd()
    {
        StopHold();
    }

    // ── Coroutine DAS ─────────────────────────────────────────────────────────

    /// <summary>
    /// Delayed Auto Shift : premier mouvement immédiat, pause initiale, puis répétition.
    ///
    /// Si la pièce disparaît en cours de hold (lockée par FallController), la coroutine
    /// se termine proprement via le check null.
    /// </summary>
    private IEnumerator HoldRepeatRoutine()
    {
        // Premier mouvement immédiat.
        _spawner.CurrentPiece?.TryMove(_holdDirection);

        yield return new WaitForSeconds(_holdInitialDelay);

        while (_isHolding)
        {
            _spawner.CurrentPiece?.TryMove(_holdDirection);
            yield return new WaitForSeconds(_holdRepeatInterval);
        }

        _holdRepeatCoroutine = null;
    }

    // ── Utilitaires privés ────────────────────────────────────────────────────

    /// <summary>Stoppe proprement la répétition DAS en cours.</summary>
    private void StopHold()
    {
        _isHolding = false;

        if (_holdRepeatCoroutine != null)
        {
            StopCoroutine(_holdRepeatCoroutine);
            _holdRepeatCoroutine = null;
        }
    }
}
