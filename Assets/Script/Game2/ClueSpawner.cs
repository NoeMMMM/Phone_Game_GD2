using System;
using UnityEngine;

/// <summary>
/// Gère l'apparition et la disparition des indices dans le Jeu 2 (Snake).
///
/// Un seul indice est présent à la fois sur la grille.
/// L'indice apparaît sur une case vide aléatoire fournie par SnakeGrid.
/// Le ramassage est signalé par SnakeCollisionHandler, qui appelle RespawnClue().
///
/// Responsabilité unique : cycle de vie d'un indice (spawn / respawn / remove).
/// Aucune détection de collision, aucune logique de queue.
/// À placer sur un GameObject "ClueSpawner" dans Game2Scene.
/// </summary>
public class ClueSpawner : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[ClueSpawner]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Grille logique du Snake. Fournit GetRandomEmptyCell, GridToWorld, SetCell.")]
    private SnakeGrid _grid;

    [SerializeField, Tooltip("Prefab de l'indice. Doit contenir un SpriteRenderer avec le sprite tilemap_94. " +
                             "Order in Layer recommandé : 8 (entre la queue à 5 et la tête à 10).")]
    private GameObject _cluePrefab;

    [SerializeField, Tooltip("Si true, spawne automatiquement un indice au Start. " +
                             "Mettre à false si c'est SnakeGameManager qui pilote le démarrage.")]
    private bool _spawnAtStart = true;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis quand un indice vient d'être placé sur la grille.
    /// Paramètre : position logique de l'indice.
    /// </summary>
    public event Action<Vector2Int> OnClueSpawned;

    /// <summary>
    /// Émis quand GetRandomEmptyCell() renvoie InvalidCell (grille pleine).
    /// SnakeGameManager peut écouter cet event pour anticiper une victoire imminente.
    /// </summary>
    public event Action OnNoEmptyCellAvailable;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Position logique de l'indice actuellement en scène.</summary>
    public Vector2Int CurrentCluePosition => _currentCluePosition;

    /// <summary>True si un indice est actuellement actif sur la grille.</summary>
    public bool HasActiveClue => _currentClue != null;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Référence au GameObject d'indice actuellement en scène. Null si aucun.</summary>
    private GameObject _currentClue;

    /// <summary>Position logique de l'indice en cours. Non significatif si _currentClue est null.</summary>
    private Vector2Int _currentCluePosition;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        if (_spawnAtStart)
            SpawnClue();
    }

    private void OnDestroy()
    {
        // Libérer la case occupée par l'indice si le script est détruit en cours de partie
        // (changement de scène, game over, etc.).
        if (_currentClue != null)
            _grid.SetCell(_currentCluePosition, CellState.Empty);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fait apparaître un indice sur une case vide aléatoire.
    /// Ne fait rien si un indice est déjà présent (appeler RespawnClue() à la place).
    /// </summary>
    public void SpawnClue()
    {
        if (_currentClue != null)
        {
            Debug.LogWarning($"{LOG_PREFIX} SpawnClue() ignoré : un indice existe déjà. " +
                             "Appeler RespawnClue() pour le remplacer.");
            return;
        }

        Vector2Int randomPos = _grid.GetRandomEmptyCell();

        // La grille est pleine — aucun emplacement disponible.
        if (randomPos == SnakeGrid.InvalidCell)
        {
            Debug.Log($"{LOG_PREFIX} Aucune case vide disponible pour spawner un indice.");
            OnNoEmptyCellAvailable?.Invoke();
            return;
        }

        _currentCluePosition = randomPos;
        Vector3 worldPos     = _grid.GridToWorld(randomPos);

        _currentClue = Instantiate(_cluePrefab, worldPos, Quaternion.identity, transform);
        _grid.SetCell(randomPos, CellState.Clue);

        Debug.Log($"{LOG_PREFIX} Indice apparu en {randomPos} (monde : {worldPos}).");
        OnClueSpawned?.Invoke(randomPos);
    }

    /// <summary>
    /// Détruit l'indice actuel et en fait apparaître un nouveau immédiatement.
    /// Appelé par SnakeCollisionHandler quand le chevalier ramasse l'indice.
    /// </summary>
    public void RespawnClue()
    {
        DestroyCurrentClue();
        SpawnClue();
    }

    /// <summary>
    /// Détruit l'indice actuel sans en respawner.
    /// Utile lors d'un game over ou d'une victoire pour nettoyer la grille.
    /// </summary>
    public void RemoveClue()
    {
        DestroyCurrentClue();
    }

    // ── Méthodes privées ──────────────────────────────────────────────────────

    /// <summary>
    /// Détruit le GameObject de l'indice actuel et libère sa case dans la grille.
    /// N'appelle pas SpawnClue — les méthodes publiques décident de la suite.
    /// </summary>
    private void DestroyCurrentClue()
    {
        if (_currentClue == null) return;

        _grid.SetCell(_currentCluePosition, CellState.Empty);
        Destroy(_currentClue);
        _currentClue = null;
    }

    /// <summary>Vérifie les dépendances obligatoires. Retourne false si l'une manque.</summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_grid == null)
        {
            Debug.LogError($"{LOG_PREFIX} _grid non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_cluePrefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} _cluePrefab non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        return isValid;
    }
}
