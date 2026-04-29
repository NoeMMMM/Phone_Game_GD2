using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère les pièces Tetris selon le système 7-bag et gère la pièce suivante.
///
/// Algorithme 7-bag :
///   - Le sac est rempli avec les 7 PieceType dans un ordre mélangé (Fisher-Yates).
///   - On retire les pièces une par une en tête de sac.
///   - Quand le sac est vide, on le remplit à nouveau.
///   - Garantit qu'aucun type n'est absent plus de 12 spawns d'affilée.
///
/// Gestion de la next piece :
///   - Au premier SpawnNextPiece(), deux pièces sont tirées :
///     la première est instanciée, la seconde est mise en réserve (next).
///   - À chaque spawn suivant, la next devient active et une nouvelle next est tirée.
///   - OnNextPieceChanged est émis à chaque changement pour mettre à jour l'UI.
///
/// Responsabilité unique : tirer des types depuis le bag, instancier TetrisPiece,
/// exposer la pièce courante et le type suivant.
/// Aucune logique de chute, aucune détection de lock, aucune gestion d'input.
/// </summary>
public class TetrisSpawner : MonoBehaviour
{
    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Prefab TetrisPiece (avec TetrisPiece.cs et _blockPrefab assigné).")]
    private GameObject _piecePrefab;

    [SerializeField, Tooltip("Grille logique Tetris de la scène.")]
    private TetrisBoard _board;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Sac courant. Vidé au fur et à mesure, refilled automatiquement.</summary>
    private List<PieceType> _bag = new List<PieceType>();

    /// <summary>Type de la prochaine pièce, déjà tiré du sac pour l'affichage UI.</summary>
    private PieceType _nextPieceType;

    /// <summary>False tant que SpawnNextPiece n'a pas encore été appelé une première fois.</summary>
    private bool _hasNextPiece;

    /// <summary>Pièce active dans la scène. Null si aucune pièce en jeu.</summary>
    private TetrisPiece _currentPiece;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>Émis après chaque spawn réussi. Paramètre : type de la pièce instanciée.</summary>
    public event Action<PieceType> OnPieceSpawned;

    /// <summary>
    /// Émis après chaque tirage d'une nouvelle next piece.
    /// TetrisHudDisplay écoute cet event pour rafraîchir l'aperçu.
    /// </summary>
    public event Action<PieceType> OnNextPieceChanged;

    /// <summary>
    /// Émis quand le spawn échoue parce que la position de spawn est déjà occupée.
    /// TetrisGameManager écoute cet event pour déclencher le game over.
    /// </summary>
    public event Action OnSpawnBlocked;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Pièce active actuelle. Null si aucune pièce en jeu ou après game over.</summary>
    public TetrisPiece CurrentPiece => _currentPiece;

    /// <summary>Type de la prochaine pièce à spawner (pour affichage UI).</summary>
    public PieceType NextPieceType => _nextPieceType;

    /// <summary>True dès qu'une next piece a été tirée (après le premier appel à SpawnNextPiece).</summary>
    public bool HasNextPiece => _hasNextPiece;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_piecePrefab == null)
            Debug.LogError("[TetrisSpawner] _piecePrefab non assigné dans l'inspecteur.", this);

        if (_board == null)
            Debug.LogError("[TetrisSpawner] _board non assigné dans l'inspecteur.", this);

        // Le spawn est déclenché explicitement par TetrisGameManager via SpawnNextPiece().
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tire la prochaine pièce du bag, l'instancie et met à jour la next piece.
    ///
    /// Premier appel : deux tirages sont effectués (pièce active + next).
    /// Appels suivants : l'ancienne next devient active, une nouvelle next est tirée.
    ///
    /// Retourne false si le spawn est bloqué (game over) — émet alors OnSpawnBlocked.
    /// </summary>
    public bool SpawnNextPiece()
    {
        // Premier appel : il faut initialiser la next piece avant de spawner.
        if (!_hasNextPiece)
        {
            _nextPieceType = DrawFromBag();
            _hasNextPiece  = true;
        }

        // La next piece devient la pièce à spawner.
        PieceType typeToSpawn = _nextPieceType;

        // Pré-tire la prochaine next piece pour l'UI.
        _nextPieceType = DrawFromBag();

        // Instancie la pièce.
        GameObject pieceGO = Instantiate(_piecePrefab);
        _currentPiece = pieceGO.GetComponent<TetrisPiece>();

        bool spawnSuccess = _currentPiece.Initialize(typeToSpawn, _board.SpawnPosition, _board);

        if (!spawnSuccess)
        {
            // La position de spawn est occupée : game over.
            Destroy(pieceGO);
            _currentPiece = null;
            OnSpawnBlocked?.Invoke();
            return false;
        }

        OnPieceSpawned?.Invoke(typeToSpawn);
        OnNextPieceChanged?.Invoke(_nextPieceType);
        return true;
    }

    /// <summary>
    /// Réinitialise le spawner : détruit la pièce active, vide le bag, reset l'état.
    /// Appelé par TetrisGameManager lors d'un game over avant de recharger la scène.
    /// </summary>
    public void ResetSpawner()
    {
        if (_currentPiece != null)
        {
            Destroy(_currentPiece.gameObject);
            _currentPiece = null;
        }

        _bag.Clear();
        _hasNextPiece = false;
    }

    // ── Bag system ────────────────────────────────────────────────────────────

    /// <summary>
    /// Retire et retourne la pièce en tête du sac.
    /// Si le sac est vide, le remplit d'abord via RefillBag().
    /// </summary>
    private PieceType DrawFromBag()
    {
        if (_bag.Count == 0)
            RefillBag();

        PieceType result = _bag[0];
        _bag.RemoveAt(0);
        return result;
    }

    /// <summary>
    /// Remplit le sac avec les 7 PieceType dans un ordre aléatoire (Fisher-Yates).
    /// </summary>
    private void RefillBag()
    {
        PieceType[] all = TetrisPieceData.GetAllTypes();

        // Mélange Fisher-Yates in-place.
        for (int i = all.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (all[i], all[j]) = (all[j], all[i]);
        }

        _bag.AddRange(all);
    }
}
