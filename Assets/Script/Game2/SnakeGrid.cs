using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Représente l'état logique de la grille de jeu du Snake (Jeu 2).
/// Fait le pont entre les coordonnées logiques (Vector2Int, origine en bas à gauche)
/// et les coordonnées monde du Tilemap Unity utilisé pour le décor.
///
/// La grille logique est un tableau 2D en mémoire de taille _gridWidth x _gridHeight.
/// Les GameObjects du chevalier, de la queue et des indices se positionnent dans le
/// monde via GridToWorld(), qui délègue à Tilemap.GetCellCenterWorld().
///
/// Responsabilité unique : état logique des cases + conversions de coordonnées.
/// Aucune logique de mouvement, de spawn ou de game over.
/// À placer sur un GameObject "SnakeGrid" dans Game2Scene.
/// </summary>
public class SnakeGrid : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SnakeGrid]";

    /// <summary>
    /// Valeur retournée par GetRandomEmptyCell() quand la grille est pleine.
    /// </summary>
    public static readonly Vector2Int InvalidCell = new Vector2Int(-1, -1);

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Tilemap de décor de la scène — utilisé uniquement pour les conversions " +
                             "de coordonnées (CellToWorld). Ne pas y écrire de tiles par script.")]
    private Tilemap _referenceTilemap;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Nombre de cases en largeur de la grille logique.")]
    private int _gridWidth = 20;

    [SerializeField, Tooltip("Nombre de cases en hauteur de la grille logique.")]
    private int _gridHeight = 10;

    [SerializeField, Tooltip("Cellule du Tilemap (coordonnées Tilemap) correspondant à la case " +
                             "logique (0, 0) de la grille de jeu. Ajuster si le décor est déplacé.")]
    private Vector2Int _gridOriginInTilemap = new Vector2Int(6, 2);

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Tableau 2D indexé [x, y], initialisé à Empty au démarrage.</summary>
    private CellState[,] _cells;

    /// <summary>
    /// Indique si Awake() s'est terminé avec succès.
    /// Bloque toutes les méthodes publiques si les dépendances sont invalides.
    /// </summary>
    private bool _isInitialized;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Nombre de cases en largeur.</summary>
    public int Width => _gridWidth;

    /// <summary>Nombre de cases en hauteur.</summary>
    public int Height => _gridHeight;

    /// <summary>
    /// Case logique au centre de la grille.
    /// Utile pour spawner le chevalier en début de partie.
    /// </summary>
    public Vector2Int Center => new Vector2Int(_gridWidth / 2, _gridHeight / 2);

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        if (_referenceTilemap == null)
        {
            Debug.LogError($"{LOG_PREFIX} _referenceTilemap non assigné dans l'inspecteur. " +
                           "SnakeGrid ne peut pas fonctionner sans Tilemap de référence.");
            return;
        }

        InitializeCells();
        _isInitialized = true;

        Debug.Log($"{LOG_PREFIX} Grille initialisée — {_gridWidth}x{_gridHeight} cases, " +
                  $"origine Tilemap : {_gridOriginInTilemap}.");
    }

    // ── Méthodes de lecture ───────────────────────────────────────────────────

    /// <summary>
    /// Retourne l'état d'une case logique.
    /// Si la position est hors grille, retourne CellState.Empty sans erreur —
    /// utiliser IsInsideGrid() en amont si une distinction est nécessaire.
    /// </summary>
    /// <param name="gridPos">Coordonnée logique de la case.</param>
    public CellState GetCell(Vector2Int gridPos)
    {
        if (!_isInitialized || !IsInsideGrid(gridPos))
            return CellState.Empty;

        return _cells[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// Indique si une coordonnée logique est dans les limites de la grille.
    /// </summary>
    public bool IsInsideGrid(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < _gridWidth
            && gridPos.y >= 0 && gridPos.y < _gridHeight;
    }

    /// <summary>
    /// Indique si une case est à l'intérieur de la grille ET libre (CellState.Empty).
    /// </summary>
    public bool IsCellEmpty(Vector2Int gridPos)
    {
        return IsInsideGrid(gridPos) && GetCell(gridPos) == CellState.Empty;
    }

    /// <summary>
    /// Retourne le nombre de cases libres (CellState.Empty) dans la grille.
    /// </summary>
    public int GetEmptyCellCount()
    {
        if (!_isInitialized) return 0;

        int count = 0;

        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                if (_cells[x, y] == CellState.Empty)
                    count++;

        return count;
    }

    /// <summary>
    /// Indique si toutes les cases de la grille sont occupées.
    /// </summary>
    public bool IsGridFull()
    {
        return GetEmptyCellCount() == 0;
    }

    /// <summary>
    /// Retourne une case logique aléatoire parmi les cases libres.
    /// Retourne InvalidCell (-1, -1) et log un warning si la grille est pleine.
    /// </summary>
    public Vector2Int GetRandomEmptyCell()
    {
        if (!_isInitialized) return InvalidCell;

        int emptyCount = GetEmptyCellCount();

        if (emptyCount == 0)
        {
            Debug.LogWarning($"{LOG_PREFIX} GetRandomEmptyCell() : aucune case vide disponible.");
            return InvalidCell;
        }

        // Sélection aléatoire par index parmi les cases vides — O(n) mais grille petite (200 cases max).
        int target = Random.Range(0, emptyCount);
        int seen   = 0;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                if (_cells[x, y] == CellState.Empty)
                {
                    if (seen == target)
                        return new Vector2Int(x, y);

                    seen++;
                }
            }
        }

        // Cas théoriquement impossible — garde-fou.
        Debug.LogError($"{LOG_PREFIX} GetRandomEmptyCell() : échec inattendu (count={emptyCount}).");
        return InvalidCell;
    }

    // ── Méthodes d'écriture ───────────────────────────────────────────────────

    /// <summary>
    /// Modifie l'état d'une case logique.
    /// Log un warning sans lever d'exception si la position est hors grille.
    /// </summary>
    /// <param name="gridPos">Coordonnée logique de la case.</param>
    /// <param name="state">Nouvel état à assigner.</param>
    public void SetCell(Vector2Int gridPos, CellState state)
    {
        if (!_isInitialized) return;

        if (!IsInsideGrid(gridPos))
        {
            Debug.LogWarning($"{LOG_PREFIX} SetCell({gridPos}, {state}) : position hors grille ignorée.");
            return;
        }

        _cells[gridPos.x, gridPos.y] = state;
    }

    /// <summary>
    /// Remet toutes les cases à CellState.Empty.
    /// À appeler depuis le gestionnaire de jeu lors d'un restart.
    /// </summary>
    public void ClearGrid()
    {
        if (!_isInitialized) return;

        InitializeCells();
    }

    // ── Méthodes de conversion ────────────────────────────────────────────────

    /// <summary>
    /// Convertit une coordonnée logique en position monde au centre de la tile correspondante.
    /// Utilise Tilemap.GetCellCenterWorld(), qui intègre le tileAnchor du Tilemap automatiquement.
    /// </summary>
    /// <param name="gridPos">Coordonnée logique (0..Width-1, 0..Height-1).</param>
    /// <returns>Position monde au centre de la tile correspondante.</returns>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3Int tilemapCell = new Vector3Int(
            gridPos.x + _gridOriginInTilemap.x,
            gridPos.y + _gridOriginInTilemap.y,
            0
        );

        return _referenceTilemap.GetCellCenterWorld(tilemapCell);
    }

    /// <summary>
    /// Convertit une position monde en coordonnée logique de la grille.
    /// Inverse de GridToWorld. Ne vérifie pas si la position est dans la grille —
    /// utiliser IsInsideGrid() sur le résultat si nécessaire.
    /// </summary>
    /// <param name="worldPos">Position monde à convertir.</param>
    /// <returns>Coordonnée logique correspondante (peut être hors grille).</returns>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3Int tilemapCell = _referenceTilemap.WorldToCell(worldPos);

        return new Vector2Int(
            tilemapCell.x - _gridOriginInTilemap.x,
            tilemapCell.y - _gridOriginInTilemap.y
        );
    }

    // ── Méthodes privées ──────────────────────────────────────────────────────

    /// <summary>
    /// Alloue et remplit le tableau _cells avec CellState.Empty.
    /// Appelé au Awake() et à chaque ClearGrid().
    /// </summary>
    private void InitializeCells()
    {
        _cells = new CellState[_gridWidth, _gridHeight];

        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                _cells[x, y] = CellState.Empty;
    }

}
