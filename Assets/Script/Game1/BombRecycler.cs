using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maintient l'état d'occupation des lanes de descente de bombes.
/// BombSpawner consulte ce script pour savoir où spawner la prochaine bombe.
/// Ne crée ni ne détruit aucune bombe.
/// </summary>
public class BombRecycler : MonoBehaviour
{
    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Nombre de lanes de descente (une par soldat)")]
    private int _laneCount = 4;

    // ── État interne ──────────────────────────────────────────────────────────

    private bool[] _laneOccupied;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        _laneOccupied = new bool[_laneCount];
    }

    private void Start()
    {
        BombFallController.OnAnyBombReachedGround += HandleBombReachedGround;
        BombFallController.OnAnyBombCaught        += HandleBombCaught;
    }

    private void OnDestroy()
    {
        BombFallController.OnAnyBombReachedGround -= HandleBombReachedGround;
        BombFallController.OnAnyBombCaught        -= HandleBombCaught;
    }

    // ── Handlers d'events statiques ───────────────────────────────────────────

    private void HandleBombReachedGround(int laneIndex) => FreeLane(laneIndex);
    private void HandleBombCaught(BombFallController bomb) => FreeLane(bomb.LaneIndex);

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Retourne true si la lane est libre (aucune bombe en descente).
    /// Retourne false si l'index est hors limites.
    /// </summary>
    public bool IsLaneFree(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= _laneCount)
        {
            Debug.LogWarning($"[BombRecycler] IsLaneFree — index {laneIndex} hors limites [0–{_laneCount - 1}].");
            return false;
        }

        return !_laneOccupied[laneIndex];
    }

    /// <summary>Marque la lane comme occupée. Appelé par BombSpawner au spawn.</summary>
    public void OccupyLane(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= _laneCount)
        {
            Debug.LogWarning($"[BombRecycler] OccupyLane — index {laneIndex} hors limites.");
            return;
        }

        _laneOccupied[laneIndex] = true;
    }

    /// <summary>Libère la lane. Appelé automatiquement via les events de BombFallController.</summary>
    public void FreeLane(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= _laneCount)
        {
            Debug.LogWarning($"[BombRecycler] FreeLane — index {laneIndex} hors limites.");
            return;
        }

        _laneOccupied[laneIndex] = false;
    }

    /// <summary>Retourne la liste des indices de lanes actuellement libres.</summary>
    public List<int> GetFreeLanes()
    {
        List<int> free = new List<int>(_laneCount);

        for (int i = 0; i < _laneCount; i++)
        {
            if (!_laneOccupied[i])
                free.Add(i);
        }

        return free;
    }
}
