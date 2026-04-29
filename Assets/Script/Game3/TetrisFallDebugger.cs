using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debugger temporaire pour TetrisFallController — À SUPPRIMER après validation.
///
/// Raccourcis clavier actifs en Play Mode (Game View focalisée) :
///   F          → Spawn la pièce suivante puis démarre la chute automatique
///   S          → Stop la chute automatique
///   ← / →      → Move Left / Right (pièce courante)
///   R           → Rotate
///   Espace      → Hard Drop
///   X           → Reset vitesse de chute
/// </summary>
public class TetrisFallDebugger : MonoBehaviour
{
    [SerializeField] private TetrisFallController _fallController;
    [SerializeField] private TetrisSpawner _spawner;

    private void Start()
    {
        if (_fallController == null || _spawner == null)
        {
            Debug.LogError("[FallDebugger] _fallController ou _spawner non assigné.", this);
            return;
        }

        _fallController.OnPieceLocked  += OnPieceLocked;
        _fallController.OnSpeedChanged += interval => Debug.Log($"[FallDebugger] OnSpeedChanged — nouvel intervalle : {interval:F3}s");

        Debug.Log("[TetrisFallDebugger] Prêt. F = Spawn+Start | S = Stop | ←→ = Move | R = Rotate | Espace = HardDrop | X = Reset vitesse");
    }

    private void OnDestroy()
    {
        if (_fallController != null)
        {
            _fallController.OnPieceLocked  -= OnPieceLocked;
            _fallController.OnSpeedChanged -= null;
        }
    }

    /// <summary>
    /// Simule ce que TetrisGameManager fera en production :
    /// spawner la pièce suivante dès qu'une pièce est lockée.
    /// </summary>
    private void OnPieceLocked()
    {
        Debug.Log("[FallDebugger] OnPieceLocked — spawn automatique de la pièce suivante.");
        bool ok = _spawner.SpawnNextPiece();

        if (!ok)
            Debug.Log("[FallDebugger] Spawn bloqué — grille pleine (game over).");
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // F : spawn la pièce courante (lock l'ancienne si elle existe) puis démarre la chute.
        if (kb.fKey.wasPressedThisFrame)
        {
            if (_spawner.CurrentPiece != null)
                _spawner.CurrentPiece.Lock();

            bool ok = _spawner.SpawnNextPiece();
            Debug.Log($"[FallDebugger] SpawnNextPiece → ok : {ok}");

            if (ok)
                _fallController.StartFalling();
        }

        // S : stop la chute automatique.
        if (kb.sKey.wasPressedThisFrame)
        {
            _fallController.StopFalling();
            Debug.Log("[FallDebugger] Chute arrêtée.");
        }

        // X : réinitialise la vitesse.
        if (kb.xKey.wasPressedThisFrame)
        {
            _fallController.ResetFallSpeed();
            Debug.Log($"[FallDebugger] Vitesse réinitialisée → {_fallController.CurrentFallInterval}s");
        }

        // Mouvement manuel de la pièce courante (pour tester pendant la chute auto).
        if (_spawner.CurrentPiece != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame)  _spawner.CurrentPiece.TryMove(Vector2Int.left);
            if (kb.rightArrowKey.wasPressedThisFrame) _spawner.CurrentPiece.TryMove(Vector2Int.right);
            if (kb.rKey.wasPressedThisFrame)          _spawner.CurrentPiece.TryRotate();

            if (kb.spaceKey.wasPressedThisFrame)
            {
                int dropped = _spawner.CurrentPiece.HardDrop();
                Debug.Log($"[FallDebugger] HardDrop — {dropped} case(s).");
            }
        }
    }
}
