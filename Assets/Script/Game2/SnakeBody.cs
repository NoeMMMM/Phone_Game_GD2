using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintient la queue de chevaliers poursuivants derrière la tête du Snake.
/// 
/// Principe : à chaque OnAboutToMove de la tête, tous les segments glissent
/// d'un cran vers l'avant. Le segment 0 prend l'ancienne position de la tête.
/// Le bout de la queue libère sa case dans la grille.
/// 
/// Option A — libération de l'ancienne position de la tête :
/// Ce script est le SEUL responsable de l'état de la case quittée par la tête.
/// - Queue vide  → libère oldHeadPos en Empty.
/// - Queue non vide → marque oldHeadPos en Tail (segment 0 vient de l'occuper).
/// SnakeMovementController ne touche plus à oldPos après OnAboutToMove.
/// 
/// Responsabilité unique : liste des segments + positionnement + état grille.
/// Aucune détection de collision, aucune logique de spawn d'indices.
/// À placer sur un GameObject "SnakeBody" dans Game2Scene.
/// </summary>
public class SnakeBody : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SnakeBody]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [SerializeField, Tooltip("Grille logique du Snake.")]
    private SnakeGrid _grid;

    [SerializeField, Tooltip("Contrôleur de la tête. Fournit OnAboutToMove.")]
    private SnakeMovementController _movementController;

    [SerializeField, Tooltip("Prefab d'un segment de queue (Sprite Renderer + sprite Knight).")]
    private GameObject _segmentPrefab;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis à chaque AddSegment(). Paramètre = nouvelle longueur totale de la queue.
    /// </summary>
    public event Action<int> OnLengthChanged;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Nombre de segments actifs dans la queue.</summary>
    public int Length => _segmentPositions.Count;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>
    /// Positions logiques des segments, du plus proche de la tête (index 0)
    /// vers le bout de la queue (index Count-1).
    /// </summary>
    private readonly List<Vector2Int> _segmentPositions = new();

    /// <summary>GameObjects instanciés, dans le même ordre que _segmentPositions.</summary>
    private readonly List<GameObject> _segmentObjects   = new();

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        _movementController.OnAboutToMove += HandleAboutToMove;
    }

    private void OnDestroy()
    {
        if (_movementController != null)
            _movementController.OnAboutToMove -= HandleAboutToMove;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Allonge la queue d'un segment.
    /// Le nouveau segment est ajouté au BOUT de la queue (index Count, le plus loin de la tête).
    /// Il prend la position du dernier segment existant, ou celle de la tête si la queue est vide.
    /// Appelé par AddSegments().
    /// </summary>
    public void AddSegment()
    {
        Vector2Int spawnPos = _segmentPositions.Count == 0
            ? _movementController.CurrentGridPosition
            : _segmentPositions[_segmentPositions.Count - 1];

        GameObject newObj = Instantiate(_segmentPrefab, _grid.GridToWorld(spawnPos), Quaternion.identity, transform);

        _segmentPositions.Add(spawnPos);
        _segmentObjects.Add(newObj);

        _grid.SetCell(spawnPos, CellState.Tail);
    }

    /// <summary>
    /// Allonge la queue de <paramref name="count"/> segments en un seul appel.
    /// Émet OnLengthChanged une seule fois après tous les ajouts.
    /// Appelé par SnakeCollisionHandler à chaque indice ramassé.
    /// </summary>
    /// <param name="count">Nombre de segments à ajouter. Ignoré si inférieur à 1.</param>
    public void AddSegments(int count)
    {
        if (count < 1) return;

        for (int i = 0; i < count; i++)
            AddSegment();

        int newLength = _segmentPositions.Count;
        Debug.Log($"{LOG_PREFIX} {count} segment(s) ajouté(s). Longueur totale : {newLength}.");
        OnLengthChanged?.Invoke(newLength);
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Indique si une position logique est occupée par un segment de queue.
    /// Utilisé par SnakeCollisionHandler pour détecter la collision tête/queue.
    /// </summary>
    public bool ContainsPosition(Vector2Int pos)
    {
        return _segmentPositions.Contains(pos);
    }

    // ── Handler mouvement ─────────────────────────────────────────────────────

    /// <summary>
    /// Appelé AVANT chaque déplacement de la tête (via OnAboutToMove).
    /// Fait glisser tous les segments d'un cran et met à jour la grille logique.
    /// 
    /// Option A — ce handler est aussi responsable de libérer oldHeadPos :
    /// - Queue vide  : libère oldHeadPos en Empty.
    /// - Queue non vide : oldHeadPos devient Tail (segment 0 vient de l'occuper),
    ///   le bout de la queue libère sa case en Empty.
    /// </summary>
    private void HandleAboutToMove(Vector2Int oldHeadPos, Vector2Int newHeadPos)
    {
        if (_segmentPositions.Count == 0)
        {
            // Pas de queue : la tête quitte simplement sa case
            _grid.SetCell(oldHeadPos, CellState.Empty);
            return;
        }

        // Sauvegarder la position du bout de queue AVANT le glissement
        Vector2Int tailTipPos = _segmentPositions[_segmentPositions.Count - 1];

        // ── Glissement : chaque segment prend la position du précédent ────────
        // On part du bout vers la tête pour ne pas écraser les valeurs en cours de lecture
        for (int i = _segmentPositions.Count - 1; i > 0; i--)
            _segmentPositions[i] = _segmentPositions[i - 1];

        // Le segment 0 (le plus proche de la tête) prend l'ancienne position de la tête
        _segmentPositions[0] = oldHeadPos;

        // ── Mise à jour des GameObjects dans le monde ─────────────────────────
        // Garde-fou null : un segment ne devrait jamais être détruit depuis l'extérieur,
        // mais on protège quand même la boucle pour éviter une NullReferenceException.
        for (int i = 0; i < _segmentObjects.Count; i++)
        {
            if (_segmentObjects[i] != null)
                _segmentObjects[i].transform.position = _grid.GridToWorld(_segmentPositions[i]);
        }

        // ── Mise à jour de la grille logique ──────────────────────────────────
        // Libérer l'ancien bout de queue
        _grid.SetCell(tailTipPos, CellState.Empty);

        // Marquer toutes les cases occupées par la queue en Tail
        // (oldHeadPos est maintenant _segmentPositions[0] → Tail, pas Empty)
        for (int i = 0; i < _segmentPositions.Count; i++)
            _grid.SetCell(_segmentPositions[i], CellState.Tail);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_grid == null)
        {
            Debug.LogError($"{LOG_PREFIX} _grid non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_movementController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _movementController non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_segmentPrefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} _segmentPrefab non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        return isValid;
    }
}
