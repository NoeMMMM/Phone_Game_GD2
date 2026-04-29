using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grille logique du Tetris (10 colonnes × 20 lignes par défaut).
///
/// Origine en bas à gauche : (0, 0) = case en bas à gauche, (9, 19) = case en haut à droite.
/// Y monte vers le haut, cohérent avec Vector2Int.up.
///
/// Chaque case contient soit null (vide) soit le Transform du bloc qui l'occupe.
/// Stocker le Transform (et non un simple enum) permet de détruire les GameObjects
/// quand une ligne est complète, sans passer par un autre système.
///
/// Responsabilité unique : état de la grille + opérations de base
/// (validation, ajout de blocs, suppression de lignes, conversion de coordonnées).
/// Aucune logique de pièce, aucune gestion d'input, aucune détection de game over.
/// À placer sur un GameObject "TetrisBoard" dans Game3Scene.
/// </summary>
public class TetrisBoard : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[TetrisBoard]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Transform marquant la position monde de la case (0, 0) " +
                             "(coin bas-gauche de la grille). Permet de positionner la grille librement.")]
    private Transform _gridOrigin;

    // ── Paramètres — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Nombre de colonnes de la grille.")]
    private int _gridWidth = 10;

    [SerializeField, Tooltip("Nombre de lignes de la grille.")]
    private int _gridHeight = 20;

    [SerializeField, Tooltip("Taille d'une case en unités Unity.")]
    private float _cellSize = 1f;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tableau 2D [x, y]. null = case vide, sinon Transform du bloc occupant.
    /// Indices : x dans [0, _gridWidth-1], y dans [0, _gridHeight-1].
    /// </summary>
    private Transform[,] _grid;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis après ClearLines(). Paramètre = nombre de lignes supprimées.
    /// TetrisGameManager écoute cet event pour mettre à jour le score.
    /// </summary>
    public event Action<int> OnLinesCleared;

    /// <summary>
    /// Émis quand un bloc est ajouté dans la ligne du haut (_gridHeight - 1).
    /// TetrisGameManager écoute cet event pour déclencher le game over.
    /// </summary>
    public event Action OnTopReached;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Nombre de colonnes de la grille.</summary>
    public int Width => _gridWidth;

    /// <summary>Nombre de lignes de la grille.</summary>
    public int Height => _gridHeight;

    /// <summary>Taille d'une case en unités Unity (nécessaire pour positionner les blocs enfants d'une pièce).</summary>
    public float CellSize => _cellSize;

    /// <summary>
    /// Position logique de spawn d'une nouvelle pièce :
    /// centre horizontal, ligne du haut.
    /// </summary>
    public Vector2Int SpawnPosition => new Vector2Int(_gridWidth / 2, _gridHeight - 1);

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        if (_gridOrigin == null)
        {
            Debug.LogError($"{LOG_PREFIX} TetrisBoard requiert un _gridOrigin assigné.", this);
            return;
        }

        _grid = new Transform[_gridWidth, _gridHeight];
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie si la position logique est à l'intérieur des limites de la grille.
    /// </summary>
    public bool IsInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _gridWidth
            && gridPos.y >= 0 && gridPos.y < _gridHeight;
    }

    /// <summary>
    /// Vérifie si la case est vide. Retourne false si hors grille.
    /// </summary>
    public bool IsCellEmpty(Vector2Int gridPos)
    {
        if (!IsInsideGrid(gridPos)) return false;
        return _grid[gridPos.x, gridPos.y] == null;
    }

    /// <summary>
    /// Vérifie que toutes les positions sont valides pour y poser des blocs.
    /// Une position est valide si elle est dans la grille ET que la case est vide.
    /// </summary>
    /// <param name="blockPositions">Positions logiques des blocs à valider.</param>
    /// <param name="allowAboveGrid">
    /// Si true, les blocs dont y >= _gridHeight sont ignorés (cas du spawn initial
    /// où une pièce peut partiellement dépasser vers le haut).
    /// </param>
    public bool IsValidPosition(Vector2Int[] blockPositions, bool allowAboveGrid = false)
    {
        foreach (Vector2Int pos in blockPositions)
        {
            // Bloc au-dessus de la grille : ignoré si allowAboveGrid, sinon invalide.
            if (pos.y >= _gridHeight)
            {
                if (allowAboveGrid) continue;
                return false;
            }

            // Hors grille latéralement ou en dessous.
            if (pos.x < 0 || pos.x >= _gridWidth || pos.y < 0)
                return false;

            // Case occupée.
            if (_grid[pos.x, pos.y] != null)
                return false;
        }

        return true;
    }

    // ── Écriture ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Enregistre un bloc dans la grille à la position logique donnée.
    /// Émet OnTopReached si le bloc est ajouté dans la ligne du haut.
    /// </summary>
    /// <param name="gridPos">Position logique cible.</param>
    /// <param name="blockTransform">Transform du bloc à enregistrer.</param>
    public void AddBlock(Vector2Int gridPos, Transform blockTransform)
    {
        if (!IsInsideGrid(gridPos))
        {
            Debug.LogWarning($"{LOG_PREFIX} AddBlock ignoré : position {gridPos} hors grille.");
            return;
        }

        _grid[gridPos.x, gridPos.y] = blockTransform;

        if (HasBlockAboveTopLine())
            OnTopReached?.Invoke();
    }

    /// <summary>
    /// Détruit tous les blocs de la grille et réinitialise toutes les cases à null.
    /// Appelé par TetrisGameManager pour réinitialiser la partie après un game over.
    /// </summary>
    public void ClearGrid()
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                if (_grid[x, y] != null)
                {
                    Destroy(_grid[x, y].gameObject);
                    _grid[x, y] = null;
                }
            }
        }
    }

    // ── Détection et suppression des lignes ───────────────────────────────────

    /// <summary>
    /// Retourne les indices Y de toutes les lignes complètes (toutes les cases occupées),
    /// triés par Y croissant (du bas vers le haut).
    /// </summary>
    public List<int> GetCompletedLines()
    {
        var completed = new List<int>();

        for (int y = 0; y < _gridHeight; y++)
        {
            if (IsLineComplete(y))
                completed.Add(y);
        }

        return completed; // Déjà trié croissant (parcours bas → haut).
    }

    /// <summary>
    /// Efface les lignes spécifiées et fait descendre par gravité tous les blocs
    /// situés au-dessus.
    ///
    /// Algorithme en deux passes :
    ///   1. Destruction : les blocs des lignes complètes sont détruits et mis à null.
    ///   2. Compaction par colonne : pour chaque colonne, un pointeur writeY part
    ///      de 0. On scanne readY de bas en haut : chaque bloc non-null est déplacé
    ///      à writeY (grille + transform.position), writeY++. Les cases restantes
    ///      au-dessus de writeY sont vidées.
    ///
    /// Gère correctement la suppression simultanée de plusieurs lignes.
    /// Émet OnLinesCleared avec le nombre de lignes effacées.
    /// </summary>
    /// <param name="lineIndices">Indices Y des lignes à effacer (croissant).</param>
    public void ClearLines(List<int> lineIndices)
    {
        if (lineIndices == null || lineIndices.Count == 0) return;

        // ── Passe 1 : destruction des blocs dans les lignes complètes ─────────
        foreach (int y in lineIndices)
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                if (_grid[x, y] != null)
                {
                    Destroy(_grid[x, y].gameObject);
                    _grid[x, y] = null;
                }
            }
        }

        // ── Passe 2 : compaction vers le bas colonne par colonne ──────────────
        for (int x = 0; x < _gridWidth; x++)
        {
            int writeY = 0;

            for (int readY = 0; readY < _gridHeight; readY++)
            {
                if (_grid[x, readY] == null) continue;

                if (readY != writeY)
                {
                    // Déplacer le bloc de readY vers writeY.
                    _grid[x, writeY] = _grid[x, readY];
                    _grid[x, readY]  = null;

                    _grid[x, writeY].position = GridToWorld(new Vector2Int(x, writeY));
                }

                writeY++;
            }

            // Vider les cases au-dessus du dernier bloc (normalement déjà null
            // après la compaction, mais on s'assure pour éviter les fantômes).
            for (int y = writeY; y < _gridHeight; y++)
                _grid[x, y] = null;
        }

        OnLinesCleared?.Invoke(lineIndices.Count);
    }

    // ── Débordement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne true si un bloc occupe au moins une case de la ligne du haut
    /// (y == _gridHeight - 1). Indique que la pile a atteint le sommet de la grille.
    /// </summary>
    public bool HasBlockAboveTopLine()
    {
        int topRow = _gridHeight - 1;

        for (int x = 0; x < _gridWidth; x++)
        {
            if (_grid[x, topRow] != null)
                return true;
        }

        return false;
    }

    // ── Conversions de coordonnées ────────────────────────────────────────────

    /// <summary>
    /// Convertit une position logique en position monde.
    /// Le résultat pointe au CENTRE de la case (pivot centre du sprite aligné).
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return _gridOrigin.position + new Vector3(
            gridPos.x * _cellSize + _cellSize * 0.5f,
            gridPos.y * _cellSize + _cellSize * 0.5f,
            0f
        );
    }

    /// <summary>
    /// Convertit une position monde en position logique (arrondie vers le bas).
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 local = worldPos - _gridOrigin.position;

        return new Vector2Int(
            Mathf.FloorToInt(local.x / _cellSize),
            Mathf.FloorToInt(local.y / _cellSize)
        );
    }

    // ── Méthodes privées utilitaires ──────────────────────────────────────────

    /// <summary>Retourne true si toutes les cases de la ligne Y sont occupées.</summary>
    private bool IsLineComplete(int y)
    {
        for (int x = 0; x < _gridWidth; x++)
        {
            if (_grid[x, y] == null)
                return false;
        }

        return true;
    }
}
