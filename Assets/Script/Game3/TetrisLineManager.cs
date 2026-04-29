using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Détecte et efface les lignes complètes après chaque verrouillage de pièce.
///
/// Flux :
///   1. TetrisSpawner émet OnPieceSpawned → TetrisLineManager s'abonne au OnLocked
///      de la pièce fraîchement instanciée.
///   2. Quand la pièce est verrouillée (par FallController OU par InputController
///      après un hard drop), OnLocked est émis.
///   3. TetrisLineManager appelle TetrisBoard.GetCompletedLines() puis ClearLines().
///   4. TetrisBoard émet OnLinesCleared(int count) → TetrisGameManager met à jour le score.
///
/// Pourquoi écouter OnPieceSpawned plutôt que FallController.OnPieceLocked :
///   FallController.OnPieceLocked n'est émis que lors d'une chute naturelle.
///   Un hard drop via TetrisInputController appelle Lock() directement —
///   FallController n'est pas impliqué. Écouter TetrisPiece.OnLocked par pièce
///   garantit que toutes les origines de lock sont couvertes.
///
/// Responsabilité unique : déclencher la vérification de lignes après chaque lock.
/// Aucune logique de score, aucune gestion d'input, aucun spawn.
/// </summary>
public class TetrisLineManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[LineManager]";

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Spawner Tetris — pour écouter OnPieceSpawned et s'abonner au OnLocked de chaque pièce.")]
    private TetrisSpawner _spawner;

    [SerializeField, Tooltip("Grille logique — pour appeler GetCompletedLines() et ClearLines().")]
    private TetrisBoard _board;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_spawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _spawner non assigné dans l'inspecteur.", this);
            return;
        }

        if (_board == null)
        {
            Debug.LogError($"{LOG_PREFIX} _board non assigné dans l'inspecteur.", this);
            return;
        }

        _spawner.OnPieceSpawned += HandlePieceSpawned;
    }

    private void OnDestroy()
    {
        if (_spawner != null)
            _spawner.OnPieceSpawned -= HandlePieceSpawned;
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé quand TetrisSpawner instancie une nouvelle pièce.
    /// S'abonne au OnLocked de cette pièce pour déclencher la vérification de lignes
    /// au moment du lock, quelle qu'en soit l'origine (chute naturelle ou hard drop).
    /// </summary>
    private void HandlePieceSpawned(PieceType type)
    {
        TetrisPiece piece = _spawner.CurrentPiece;

        if (piece == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} HandlePieceSpawned : CurrentPiece est null après OnPieceSpawned.", this);
            return;
        }

        // Abonnement one-shot : le lambda capture la référence à la pièce
        // pour pouvoir se désabonner proprement après le premier fire.
        // Utiliser un champ Action local permet de référencer le même delegate
        // pour le -= dans le corps du lambda lui-même.
        System.Action<Vector2Int[]> onLockedHandler = null;
        onLockedHandler = lockedPositions =>
        {
            piece.OnLocked -= onLockedHandler;
            CheckAndClearLines(lockedPositions);
        };

        piece.OnLocked += onLockedHandler;
    }

    /// <summary>
    /// Interroge la grille pour détecter les lignes complètes et les efface.
    /// TetrisBoard.ClearLines() émet OnLinesCleared(int count) pour le score.
    /// </summary>
    /// <param name="lockedPositions">
    /// Positions des blocs venant d'être verrouillés (non utilisé directement —
    /// on interroge la grille entière pour gérer les combos multi-lignes).
    /// </param>
    private void CheckAndClearLines(Vector2Int[] lockedPositions)
    {
        List<int> completedLines = _board.GetCompletedLines();

        if (completedLines.Count == 0)
            return;

        Debug.Log($"{LOG_PREFIX} {completedLines.Count} ligne(s) complète(s) détectée(s) : [{string.Join(", ", completedLines)}]");

        _board.ClearLines(completedLines);
    }
}
