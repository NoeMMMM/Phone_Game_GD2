using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debugger temporaire pour TetrisSpawner — À SUPPRIMER après validation.
///
/// Raccourcis clavier actifs en Play Mode (Game View focalisée) :
///   N          → Lock la pièce courante (si elle existe) puis Spawn Next
///   ← / →      → Move Left / Right
///   ↓           → Move Down (une case)
///   R           → Rotate horaire
///   Espace      → Hard Drop
///   X           → Reset le spawner
/// </summary>
public class TetrisSpawnerDebugger : MonoBehaviour
{
    [SerializeField] private TetrisSpawner _spawner;

    private void Start()
    {
        if (_spawner == null)
        {
            Debug.LogError("[TetrisSpawnerDebugger] _spawner non assigné.", this);
            return;
        }

        _spawner.OnPieceSpawned     += type => Debug.Log($"[SpawnerDebugger] OnPieceSpawned : {type}");
        _spawner.OnNextPieceChanged += type => Debug.Log($"[SpawnerDebugger] OnNextPieceChanged : {type}");
        _spawner.OnSpawnBlocked     += ()   => Debug.Log("[SpawnerDebugger] OnSpawnBlocked — grille pleine au sommet.");

        Debug.Log("[TetrisSpawnerDebugger] Prêt. N = Lock+Spawn | ←→↓ = Move | R = Rotate | Espace = HardDrop | X = Reset");
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // N : lock la pièce courante si elle existe, puis spawne la suivante.
        // Simule le flux normal du jeu : Lock → Spawn.
        if (kb.nKey.wasPressedThisFrame)
        {
            if (_spawner.CurrentPiece != null)
                _spawner.CurrentPiece.Lock();

            bool ok = _spawner.SpawnNextPiece();
            Debug.Log($"[SpawnerDebugger] SpawnNextPiece → ok : {ok} | next : {(_spawner.HasNextPiece ? _spawner.NextPieceType.ToString() : "—")}");
        }

        // Mouvement de la pièce courante.
        if (_spawner.CurrentPiece != null)
        {
            if (kb.leftArrowKey.wasPressedThisFrame)  _spawner.CurrentPiece.TryMove(Vector2Int.left);
            if (kb.rightArrowKey.wasPressedThisFrame) _spawner.CurrentPiece.TryMove(Vector2Int.right);
            if (kb.downArrowKey.wasPressedThisFrame)  _spawner.CurrentPiece.TryMove(Vector2Int.down);
            if (kb.rKey.wasPressedThisFrame)          _spawner.CurrentPiece.TryRotate();

            if (kb.spaceKey.wasPressedThisFrame)
            {
                int dropped = _spawner.CurrentPiece.HardDrop();
                Debug.Log($"[SpawnerDebugger] HardDrop — {dropped} case(s).");
            }
        }

        // X : reset complet.
        if (kb.xKey.wasPressedThisFrame)
        {
            _spawner.ResetSpawner();
            Debug.Log("[SpawnerDebugger] Spawner réinitialisé.");
        }
    }
}
