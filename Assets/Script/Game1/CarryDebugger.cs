using UnityEngine;

/// <summary>
/// Script de débogage temporaire pour valider BombCarryController et ExplosionController.
/// À supprimer une fois le Jeu 1 validé en Play Mode.
/// À placer sur n'importe quel GameObject de la scène (ex : /Grid ou un /DebugHelper dédié).
/// </summary>
public class CarryDebugger : MonoBehaviour
{
    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Référence au BombCarryController sur le GameObject /Player")]
    private BombCarryController _carryController;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_carryController == null)
        {
            Debug.LogError("[CarryDebugger] _carryController non assigné — glisser le composant depuis /Player.");
            return;
        }

        _carryController.OnBombCountChanged += HandleBombCountChanged;
        _carryController.OnBombsThrown      += HandleBombsThrown;

        Debug.Log("[CarryDebugger] Abonnements actifs sur BombCarryController.");
    }

    private void OnDestroy()
    {
        if (_carryController == null) return;

        _carryController.OnBombCountChanged -= HandleBombCountChanged;
        _carryController.OnBombsThrown      -= HandleBombsThrown;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    /// <summary>Logué à chaque attrape ou lancer.</summary>
    private void HandleBombCountChanged(int count)
    {
        Debug.Log($"[CarryDebugger] OnBombCountChanged → Bombes en main : {count}");
    }

    /// <summary>Logué quand le chevalier lance ses bombes sur la porte.</summary>
    private void HandleBombsThrown(int count)
    {
        Debug.Log($"[CarryDebugger] OnBombsThrown → {count} bombe(s) envoyée(s) sur la porte !");
    }
}
