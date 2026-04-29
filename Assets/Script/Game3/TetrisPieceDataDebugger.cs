using UnityEngine;

/// <summary>
/// Script temporaire de test — À SUPPRIMER après validation de TetrisPieceData.
///
/// Utilise un ContextMenu pour logger tous les offsets et couleurs des 7 pièces.
/// À placer sur n'importe quel GameObject en scène.
/// </summary>
public class TetrisPieceDataDebugger : MonoBehaviour
{
    /// <summary>
    /// Affiche les offsets et couleurs des 7 pièces dans la console Unity.
    /// Clic droit sur le composant → "Test pieces".
    /// </summary>
    [ContextMenu("Test pieces")]
    private void TestPieces()
    {
        foreach (PieceType type in TetrisPieceData.GetAllTypes())
        {
            Debug.Log($"=== Pièce {type} (couleur : {TetrisPieceData.GetColor(type)}) ===");

            for (int rot = 0; rot < 4; rot++)
            {
                string offsets = string.Join(", ", TetrisPieceData.GetOffsets(type, rot));
                Debug.Log($"  Rotation {rot} : {offsets}");
            }
        }
    }
}
