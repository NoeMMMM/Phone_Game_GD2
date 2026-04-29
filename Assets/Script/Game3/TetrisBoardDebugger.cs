using UnityEngine;

/// <summary>
/// Script temporaire de test — À SUPPRIMER après validation de TetrisBoard.
///
/// Expose des méthodes ContextMenu pour tester les conversions de coordonnées,
/// l'ajout de blocs, la détection et la suppression des lignes complètes.
/// À placer sur un GameObject "_DEBUG_Board" dans Game3Scene.
/// </summary>
public class TetrisBoardDebugger : MonoBehaviour
{
    [SerializeField] private TetrisBoard _board;
    [SerializeField] private GameObject  _testBlockPrefab;

    [ContextMenu("Test conversions")]
    private void TestConversions()
    {
        Debug.Log($"[BoardDebugger] World pos de (0,0)  : {_board.GridToWorld(Vector2Int.zero)}");
        Debug.Log($"[BoardDebugger] World pos de (5,10) : {_board.GridToWorld(new Vector2Int(5, 10))}");
        Debug.Log($"[BoardDebugger] Spawn position       : {_board.SpawnPosition}");
    }

    [ContextMenu("Add test block at spawn")]
    private void AddBlockAtSpawn()
    {
        Vector2Int pos   = _board.SpawnPosition;
        GameObject block = Instantiate(_testBlockPrefab, _board.GridToWorld(pos), Quaternion.identity);
        _board.AddBlock(pos, block.transform);
        Debug.Log($"[BoardDebugger] Bloc ajouté à {pos}.");
    }

    [ContextMenu("Fill bottom row")]
    private void FillBottomRow()
    {
        for (int x = 0; x < _board.Width; x++)
        {
            Vector2Int pos   = new Vector2Int(x, 0);
            GameObject block = Instantiate(_testBlockPrefab, _board.GridToWorld(pos), Quaternion.identity);
            _board.AddBlock(pos, block.transform);
        }

        Debug.Log("[BoardDebugger] Ligne du bas remplie.");
    }

    [ContextMenu("Get completed lines")]
    private void TestCompletedLines()
    {
        var lines = _board.GetCompletedLines();
        Debug.Log($"[BoardDebugger] Lignes complètes : {lines.Count} → [{string.Join(", ", lines)}]");
    }

    [ContextMenu("Clear completed lines")]
    private void ClearLines()
    {
        var lines = _board.GetCompletedLines();
        _board.ClearLines(lines);
        Debug.Log($"[BoardDebugger] Lignes effacées : {lines.Count}.");
    }
}
