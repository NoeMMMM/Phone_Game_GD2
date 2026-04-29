using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Représente une pièce Tetris active dans la scène.
///
/// Responsabilité unique : porter l'état de la pièce (type, position pivot,
/// rotation) et exposer les opérations de déplacement, rotation, hard drop et
/// verrouillage. Aucune gestion d'input ni de chute automatique — ces rôles
/// appartiennent respectivement à TetrisInputController et TetrisFallController.
///
/// Architecture visuelle :
///   - ce GameObject = pivot de la pièce, positionné dans la scène monde.
///   - 4 enfants "Block" = les sprites carrés, positionnés en localPosition
///     selon les offsets de la rotation courante × CellSize.
///
/// Durée de vie : instancié par TetrisSpawner, détruit dans Lock() après
/// avoir détaché ses blocs dans la grille.
/// </summary>
public class TetrisPiece : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[TetrisPiece]";

    /// <summary>Nombre de blocs par pièce (invariant Tetris).</summary>
    private const int BLOCK_COUNT = 4;

    /// <summary>Nombre de rotations possibles.</summary>
    private const int ROTATION_COUNT = 4;

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Prefab du bloc unitaire (GameObject avec SpriteRenderer). " +
                             "Instancié 4 fois comme enfants de cette pièce.")]
    private GameObject _blockPrefab;

    [SerializeField, Tooltip("Référence à la grille logique. " +
                             "Peut être assignée ici ou injectée via Initialize().")]
    private TetrisBoard _board;

    // ── État interne ──────────────────────────────────────────────────────────

    private PieceType _type;
    private Vector2Int _pivotGridPosition;
    private int _currentRotation;

    /// <summary>
    /// Transforms des 4 blocs enfants, dans l'ordre des offsets TetrisPieceData.
    /// Peuplé dans Initialize().
    /// </summary>
    private List<Transform> _blockTransforms = new List<Transform>(BLOCK_COUNT);

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>Émis après chaque déplacement réussi.</summary>
    public event Action OnMoved;

    /// <summary>Émis après chaque rotation réussie.</summary>
    public event Action OnRotated;

    /// <summary>
    /// Émis dans Lock(), juste avant la destruction de la pièce.
    /// Paramètre : positions logiques des blocs verrouillés dans la grille.
    /// TetrisLineManager écoute cet event pour déclencher la détection de lignes.
    /// </summary>
    public event Action<Vector2Int[]> OnLocked;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Type de pièce (I, O, T, L, J, S, Z).</summary>
    public PieceType Type => _type;

    /// <summary>Position pivot actuelle dans la grille logique.</summary>
    public Vector2Int PivotGridPosition => _pivotGridPosition;

    /// <summary>
    /// Transforms des 4 blocs enfants.
    /// Utilisé par TetrisBoard.AddBlock lors du verrouillage.
    /// Les appelants ne doivent pas modifier cette liste.
    /// </summary>
    public List<Transform> BlockTransforms => _blockTransforms;

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise la pièce avec son type, sa position de spawn et la référence à la grille.
    /// Instancie les 4 blocs enfants, applique la couleur, positionne visuellement.
    ///
    /// Retourne false si le spawn est bloqué (game over immédiat) : la position de spawn
    /// est déjà occupée par des blocs verrouillés.
    /// </summary>
    /// <param name="type">Type de pièce à initialiser.</param>
    /// <param name="spawnGridPos">Position pivot de départ dans la grille.</param>
    /// <param name="board">Grille logique (écrase la référence inspecteur si fournie).</param>
    /// <returns>True si le spawn est valide, false si bloqué.</returns>
    public bool Initialize(PieceType type, Vector2Int spawnGridPos, TetrisBoard board)
    {
        _type             = type;
        _pivotGridPosition = spawnGridPos;
        _currentRotation  = 0;
        _board            = board;

        SpawnBlocks();
        UpdateVisualPositions();

        // Vérifie que la position initiale est libre — si non, c'est un game over.
        bool spawnValid = _board.IsValidPosition(GetCurrentBlockPositions(), allowAboveGrid: true);

        if (!spawnValid)
            Debug.Log($"{LOG_PREFIX} Spawn bloqué pour la pièce {_type} — game over détecté.");

        return spawnValid;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcule et retourne les 4 positions logiques absolues des blocs
    /// en appliquant les offsets de la rotation courante au pivot.
    /// Crée un nouveau tableau à chaque appel (pas de référence partagée).
    /// </summary>
    public Vector2Int[] GetCurrentBlockPositions()
    {
        return ComputeBlockPositions(_pivotGridPosition, _currentRotation);
    }

    // ── Mouvements ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tente de déplacer la pièce dans la direction donnée.
    /// Valide la nouvelle position via TetrisBoard avant d'appliquer.
    /// </summary>
    /// <param name="direction">Déplacement en cases (ex. Vector2Int.left, Vector2Int.down).</param>
    /// <returns>True si le déplacement a été appliqué.</returns>
    public bool TryMove(Vector2Int direction)
    {
        Vector2Int newPivot = _pivotGridPosition + direction;
        Vector2Int[] newPositions = ComputeBlockPositions(newPivot, _currentRotation);

        if (!_board.IsValidPosition(newPositions, allowAboveGrid: true))
            return false;

        _pivotGridPosition = newPivot;
        UpdateVisualPositions();
        OnMoved?.Invoke();
        return true;
    }

    /// <summary>
    /// Tente de faire descendre la pièce d'une case.
    /// Raccourci vers TryMove(Vector2Int.down).
    /// </summary>
    /// <returns>True si la descente a été appliquée.</returns>
    public bool TryDropOne()
    {
        return TryMove(Vector2Int.down);
    }

    /// <summary>
    /// Tente une rotation horaire (0→1→2→3→0).
    /// Si la position résultante est invalide, la rotation est ignorée (pas de wall kicks).
    /// </summary>
    /// <returns>True si la rotation a été appliquée.</returns>
    public bool TryRotate()
    {
        int newRotation = (_currentRotation + 1) % ROTATION_COUNT;
        Vector2Int[] newPositions = ComputeBlockPositions(_pivotGridPosition, newRotation);

        if (!_board.IsValidPosition(newPositions, allowAboveGrid: true))
            return false;

        _currentRotation = newRotation;
        UpdateVisualPositions();
        OnRotated?.Invoke();

        // Feedback audio/visuel — FeedbackManager peut être absent en tests isolation.
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayFeedback(FeedbackType.TetrisRotate);

        return true;
    }

    /// <summary>
    /// Fait descendre la pièce case par case jusqu'à ce qu'elle ne puisse plus avancer.
    /// Le verrouillage doit être déclenché explicitement par l'appelant (via Lock()).
    /// </summary>
    /// <returns>Nombre de cases descendues.</returns>
    public int HardDrop()
    {
        int dropped = 0;

        while (TryDropOne())
            dropped++;

        return dropped;
    }

    // ── Verrouillage ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verrouille la pièce dans la grille :
    ///   1. Enregistre chaque bloc dans TetrisBoard.AddBlock.
    ///   2. Détache les blocs du parent pour qu'ils survivent à la destruction de la pièce.
    ///   3. Émet OnLocked avec les positions verrouillées.
    ///   4. Détruit ce GameObject.
    ///
    /// Les blocs hors grille (y >= Height, cas d'un spawn partiel) ne sont pas enregistrés
    /// — TetrisBoard.AddBlock les rejetterait de toute façon.
    /// </summary>
    public void Lock()
    {
        Vector2Int[] offsets = TetrisPieceData.GetOffsets(_type, _currentRotation);
        Vector2Int[] lockedPositions = new Vector2Int[BLOCK_COUNT];

        for (int i = 0; i < BLOCK_COUNT; i++)
        {
            Vector2Int gridPos = _pivotGridPosition + offsets[i];
            lockedPositions[i] = gridPos;

            Transform blockTransform = _blockTransforms[i];

            // Détache avant AddBlock pour éviter que la destruction du parent
            // n'invalide la référence dans la grille.
            blockTransform.SetParent(null);

            if (_board.IsInsideGrid(gridPos))
                _board.AddBlock(gridPos, blockTransform);
            else
                Destroy(blockTransform.gameObject); // Bloc hors grille : nettoyage.
        }

        OnLocked?.Invoke(lockedPositions);
        Destroy(gameObject);
    }

    // ── Méthodes privées ──────────────────────────────────────────────────────

    /// <summary>
    /// Instancie les 4 blocs enfants depuis _blockPrefab et applique la couleur du type.
    /// </summary>
    private void SpawnBlocks()
    {
        Color pieceColor = TetrisPieceData.GetColor(_type);

        for (int i = 0; i < BLOCK_COUNT; i++)
        {
            GameObject block = Instantiate(_blockPrefab, this.transform);
            block.name = $"Block_{i}";

            // Applique la couleur de teinte sur le SpriteRenderer.
            SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = pieceColor;
            else
                Debug.LogWarning($"{LOG_PREFIX} Le _blockPrefab ne possède pas de SpriteRenderer.", block);

            _blockTransforms.Add(block.transform);
        }
    }

    /// <summary>
    /// Calcule les 4 positions logiques absolues pour un pivot et une rotation donnés,
    /// sans modifier l'état de la pièce. Utilisé pour la validation avant d'appliquer.
    /// </summary>
    /// <param name="pivot">Position pivot à utiliser pour le calcul.</param>
    /// <param name="rotation">Index de rotation à utiliser.</param>
    private Vector2Int[] ComputeBlockPositions(Vector2Int pivot, int rotation)
    {
        Vector2Int[] offsets = TetrisPieceData.GetOffsets(_type, rotation);
        Vector2Int[] positions = new Vector2Int[BLOCK_COUNT];

        for (int i = 0; i < BLOCK_COUNT; i++)
            positions[i] = pivot + offsets[i];

        return positions;
    }

    /// <summary>
    /// Met à jour la position monde du pivot parent et les localPositions des 4 blocs enfants
    /// selon les offsets de la rotation courante.
    ///
    /// GridToWorld() retourne le centre de la case pivot.
    /// Les localPositions des enfants sont en multiples de CellSize pour coïncider avec
    /// les centres des cases adjacentes.
    /// </summary>
    private void UpdateVisualPositions()
    {
        this.transform.position = _board.GridToWorld(_pivotGridPosition);

        Vector2Int[] offsets = TetrisPieceData.GetOffsets(_type, _currentRotation);
        float cellSize = _board.CellSize;

        for (int i = 0; i < BLOCK_COUNT; i++)
        {
            _blockTransforms[i].localPosition = new Vector3(
                offsets[i].x * cellSize,
                offsets[i].y * cellSize,
                0f
            );
        }
    }
}
