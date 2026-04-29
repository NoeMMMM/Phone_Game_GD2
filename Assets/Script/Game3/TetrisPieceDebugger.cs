using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debugger temporaire pour TetrisPiece — À SUPPRIMER après validation.
///
/// Raccourcis clavier actifs en Play Mode (Game View focalisée) :
///   I / O / T / L / J / S / Z  → Spawn la pièce correspondante
///   ← / →                      → Move Left / Right
///   ↓                           → Move Down (une case)
///   R                           → Rotate horaire
///   Espace                      → Hard Drop
///   Entrée                      → Lock
///
/// Utilise le New Input System (Keyboard.current) — cohérent avec le reste du projet.
/// </summary>
public class TetrisPieceDebugger : MonoBehaviour
{
    [SerializeField] private GameObject _piecePrefab;
    [SerializeField] private TetrisBoard _board;

    private TetrisPiece _currentPiece;

    private void Start()
    {
        Debug.Log("[TetrisPieceDebugger] Prêt. I/O/T/L/J/S/Z = Spawn | ←→↓ = Move | R = Rotate | Espace = HardDrop | Entrée = Lock");
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // Spawns
        if (kb.iKey.wasPressedThisFrame) SpawnPiece(PieceType.I);
        if (kb.oKey.wasPressedThisFrame) SpawnPiece(PieceType.O);
        if (kb.tKey.wasPressedThisFrame) SpawnPiece(PieceType.T);
        if (kb.lKey.wasPressedThisFrame) SpawnPiece(PieceType.L);
        if (kb.jKey.wasPressedThisFrame) SpawnPiece(PieceType.J);
        if (kb.sKey.wasPressedThisFrame) SpawnPiece(PieceType.S);
        if (kb.zKey.wasPressedThisFrame) SpawnPiece(PieceType.Z);

        // Mouvements
        if (kb.leftArrowKey.wasPressedThisFrame)  TryAction(() => _currentPiece.TryMove(Vector2Int.left),  "Move Left");
        if (kb.rightArrowKey.wasPressedThisFrame) TryAction(() => _currentPiece.TryMove(Vector2Int.right), "Move Right");
        if (kb.downArrowKey.wasPressedThisFrame)  TryAction(() => _currentPiece.TryMove(Vector2Int.down),  "Move Down");
        if (kb.rKey.wasPressedThisFrame)          TryAction(() => _currentPiece.TryRotate(),               "Rotate");
        if (kb.spaceKey.wasPressedThisFrame)      HardDrop();
        if (kb.enterKey.wasPressedThisFrame)      Lock();
    }

    private void SpawnPiece(PieceType type)
    {
        if (_piecePrefab == null || _board == null)
        {
            Debug.LogError("[TetrisPieceDebugger] _piecePrefab ou _board non assigné dans l'inspecteur.");
            return;
        }

        if (_currentPiece != null)
            Destroy(_currentPiece.gameObject);

        GameObject go = Instantiate(_piecePrefab);
        _currentPiece = go.GetComponent<TetrisPiece>();

        if (_currentPiece == null)
        {
            Debug.LogError("[TetrisPieceDebugger] Le prefab ne contient pas de composant TetrisPiece.");
            Destroy(go);
            return;
        }

        _currentPiece.OnMoved   += () => Debug.Log($"[TetrisPieceDebugger] OnMoved — pivot : {_currentPiece.PivotGridPosition}");
        _currentPiece.OnRotated += () => Debug.Log("[TetrisPieceDebugger] OnRotated");

        bool valid = _currentPiece.Initialize(type, _board.SpawnPosition, _board);
        Debug.Log($"[TetrisPieceDebugger] Spawn {type} — valide : {valid}");
    }

    private void HardDrop()
    {
        if (!CheckPiece()) return;
        int dropped = _currentPiece.HardDrop();
        Debug.Log($"[TetrisPieceDebugger] HardDrop — {dropped} case(s).");
    }

    private void Lock()
    {
        if (!CheckPiece()) return;
        _currentPiece.OnLocked += positions =>
            Debug.Log($"[TetrisPieceDebugger] OnLocked — blocs : {string.Join(", ", positions)}");
        _currentPiece.Lock();
        _currentPiece = null;
    }

    private bool CheckPiece()
    {
        if (_currentPiece != null) return true;
        Debug.LogError("[TetrisPieceDebugger] Aucune pièce active. Appuie sur I/O/T/L/J/S/Z.");
        return false;
    }

    private void TryAction(System.Func<bool> action, string label)
    {
        if (!CheckPiece()) return;
        bool result = action();
        Debug.Log($"[TetrisPieceDebugger] {label} — {(result ? "OK" : "bloqué")}.");
    }
}
