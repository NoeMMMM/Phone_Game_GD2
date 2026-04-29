using UnityEngine;

/// <summary>
/// Réagit aux explosions de bombes au sol et désoriente toujours le chevalier.
/// Toute bombe non rattrapée qui touche le sol inverse les contrôles, quelle que soit
/// la position du chevalier — cohérent avec le gameplay Game & Watch voulu.
/// S'abonne à BombFallController.OnAnyBombReachedGround.
/// À placer sur le GameObject /Player.
/// </summary>
public class ExplosionController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[ExplosionController]";

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Référence au KnightPositionController du chevalier")]
    private KnightPositionController _knightController;

    [SerializeField, Tooltip("Durée en secondes de l'inversion des contrôles après une explosion")]
    private float _disorientationDuration = 1.5f;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateDependencies()) return;

        BombFallController.OnAnyBombReachedGround += HandleBombReachedGround;
    }

    private void OnDestroy()
    {
        BombFallController.OnAnyBombReachedGround -= HandleBombReachedGround;
    }

    // ── Handler d'explosion ───────────────────────────────────────────────────

    /// <summary>
    /// Appelé quand une bombe touche le sol sans avoir été rattrapée.
    /// Désoriente toujours le chevalier, quelle que soit sa position.
    /// </summary>
    /// <param name="laneIndex">Index de la lane (0–3) où la bombe a explosé.</param>
    private void HandleBombReachedGround(int laneIndex)
    {
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.BombExploded, transform.position);
        _knightController.SetControlsInverted(_disorientationDuration);

        Debug.Log($"{LOG_PREFIX} Bombe explosée lane {laneIndex} — " +
                  $"chevalier désorienté {_disorientationDuration}s.");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateDependencies()
    {
        if (_knightController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _knightController non assigné dans l'inspecteur.");
            return false;
        }

        return true;
    }
}
