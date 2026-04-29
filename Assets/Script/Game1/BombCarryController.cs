using System;
using UnityEngine;

/// <summary>
/// Gère les bombes portées par le chevalier (max 3) et leur lancer vers la porte.
/// Détecte l'attrape via BombFallController.OnAnyBombReachedCatchZone — aucun Update.
/// Ne gère ni l'explosion, ni le score, ni la victoire.
/// À placer sur le GameObject /Player.
/// </summary>
public class BombCarryController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[BombCarryController]";

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Référence au KnightPositionController du même GameObject")]
    private KnightPositionController _knightController;

    [SerializeField, Tooltip("Nombre maximum de bombes portables simultanément")]
    private int _maxBombs = 3;

    [SerializeField, Tooltip("Mapping lane → index de position du chevalier. " +
                             "Lane 0 = pos 0, Lane 1 = pos 1, Lane 2 = pos 3, Lane 3 = pos 4.")]
    private int[] _laneToPositionIndex = new int[] { 0, 1, 3, 4 };

    // ── État interne ──────────────────────────────────────────────────────────

    private int _currentBombs;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis à chaque modification du nombre de bombes portées.
    /// Paramètre : nouveau compteur (0–_maxBombs).
    /// Consommé par HudDisplay pour mettre à jour l'UI.
    /// </summary>
    public event Action<int> OnBombCountChanged;

    /// <summary>
    /// Émis quand le chevalier lance ses bombes sur la porte.
    /// Paramètre : nombre de bombes lancées (toujours == _maxBombs au lancer).
    /// Consommé par BombCountManager pour incrémenter le compteur de porte.
    /// </summary>
    public event Action<int> OnBombsThrown;

    // ── Propriété publique ────────────────────────────────────────────────────

    /// <summary>Nombre de bombes actuellement portées par le chevalier (0–_maxBombs).</summary>
    public int CurrentBombs => _currentBombs;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateDependencies()) return;

        BombFallController.OnAnyBombReachedCatchZone += HandleBombReachedCatchZone;
        _knightController.OnSwipeUp                  += HandleSwipeUp;
    }

    private void OnDestroy()
    {
        BombFallController.OnAnyBombReachedCatchZone -= HandleBombReachedCatchZone;

        if (_knightController != null)
            _knightController.OnSwipeUp -= HandleSwipeUp;
    }

    // ── Handler de zone d'attrape ─────────────────────────────────────────────

    /// <summary>
    /// Appelé quand une bombe atteint l'avant-dernier point de descente.
    /// Vérifie si le chevalier est sur la bonne position et peut encore porter des bombes.
    /// L'event n'est émis qu'une fois par bombe — aucun guard anti-doublon nécessaire.
    /// </summary>
    private void HandleBombReachedCatchZone(BombFallController bomb)
    {
        // Mains pleines : impossible d'attraper
        if (_currentBombs >= _maxBombs) return;

        int laneIndex = bomb.LaneIndex;

        // Lane hors plage du mapping
        if (laneIndex < 0 || laneIndex >= _laneToPositionIndex.Length)
        {
            Debug.LogWarning($"{LOG_PREFIX} LaneIndex {laneIndex} hors plage du mapping _laneToPositionIndex.");
            return;
        }

        int expectedPosition = _laneToPositionIndex[laneIndex];

        // Chevalier pas sur la position correspondant à cette lane
        if (_knightController.CurrentIndex != expectedPosition) return;

        // Attrape validée : GetCaught() lève _isBeingCaught sur la bombe → OnAnyBombCaught →
        // BombRecycler libère la lane automatiquement → Destroy(gameObject) sur la bombe.
        bomb.GetCaught();

        _currentBombs++;
        OnBombCountChanged?.Invoke(_currentBombs);

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.BombCaught, transform.position);
        Debug.Log($"{LOG_PREFIX} Bombe attrapée (lane {laneIndex}, position {expectedPosition}). " +
                  $"En main : {_currentBombs}/{_maxBombs}");
    }

    // ── Handler swipe haut ────────────────────────────────────────────────────

    /// <summary>
    /// Appelé quand KnightPositionController émet OnSwipeUp.
    /// Lance les bombes si le chevalier est au centre et porte au moins une bombe.
    /// </summary>
    private void HandleSwipeUp()
    {
        if (_currentBombs <= 0)
        {
            Debug.Log($"{LOG_PREFIX} Swipe up ignoré — aucune bombe en main.");
            return;
        }

        if (!_knightController.IsAtCenter)
        {
            Debug.Log($"{LOG_PREFIX} Swipe up ignoré — chevalier pas au centre (index {_knightController.CurrentIndex}).");
            return;
        }

        ThrowBombs();
    }

    /// <summary>
    /// Lance les bombes portées sur la porte et remet le compteur à zéro.
    /// </summary>
    private void ThrowBombs()
    {
        int thrown = _currentBombs;

        _currentBombs = 0;

        OnBombsThrown?.Invoke(thrown);
        OnBombCountChanged?.Invoke(_currentBombs);

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.BombThrown, transform.position);
        Debug.Log($"{LOG_PREFIX} {thrown} bombe(s) lancée(s) sur la porte !");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateDependencies()
    {
        if (_knightController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _knightController non assigné dans l'inspecteur.");
            return false;
        }

        if (_laneToPositionIndex == null || _laneToPositionIndex.Length == 0)
        {
            Debug.LogError($"{LOG_PREFIX} _laneToPositionIndex est vide ou null.");
            return false;
        }

        return true;
    }
}
