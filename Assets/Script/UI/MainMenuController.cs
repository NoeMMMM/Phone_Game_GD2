using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôleur du menu principal.
/// Affiche le titre du jeu, écoute le bouton Jouer (délègue à GameFlowManager)
/// et le bouton Leaderboard (active le panel overlay).
/// Pas singleton, pas persistant — appartient à MainMenuScene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private const string LOG_PREFIX = "[MainMenuController]";

    // ── Références UI ─────────────────────────────────────────────────────────

    [SerializeField, Tooltip("Texte du titre du jeu affiché en haut de l'écran.")]
    private TextMeshProUGUI _gameTitleText;

    [SerializeField, Tooltip("Bouton Jouer — lance une nouvelle partie via GameFlowManager.")]
    private Button _playButton;

    [SerializeField, Tooltip("Bouton Leaderboard — affiche le panel leaderboard en overlay.")]
    private Button _leaderboardButton;

    [SerializeField, Tooltip("Panel overlay du leaderboard. Désactivé par défaut, " +
                             "activé au clic sur Leaderboard. Sa fermeture est gérée par LeaderboardDisplay.")]
    private GameObject _leaderboardPanel;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        _leaderboardPanel.SetActive(false);

        _playButton.onClick.AddListener(HandlePlayClicked);
        _leaderboardButton.onClick.AddListener(HandleLeaderboardClicked);
    }

    private void OnDestroy()
    {
        if (_playButton != null)
            _playButton.onClick.RemoveListener(HandlePlayClicked);

        if (_leaderboardButton != null)
            _leaderboardButton.onClick.RemoveListener(HandleLeaderboardClicked);
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandlePlayClicked()
    {
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);

        if (GameFlowManager.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} GameFlowManager.Instance est null. " +
                           "Vérifiez que _PersistentManagers est présent dans MainMenuScene.");
            return;
        }

        GameFlowManager.Instance.StartNewGame();
    }

    private void HandleLeaderboardClicked()
    {
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);
        _leaderboardPanel.SetActive(true);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que toutes les références SerializeField sont assignées.
    /// Retourne false et log une erreur par référence manquante.
    /// </summary>
    private bool ValidateReferences()
    {
        bool valid = true;

        if (_gameTitleText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _gameTitleText non assigné dans l'inspecteur.");
            valid = false;
        }

        if (_playButton == null)
        {
            Debug.LogError($"{LOG_PREFIX} _playButton non assigné dans l'inspecteur.");
            valid = false;
        }

        if (_leaderboardButton == null)
        {
            Debug.LogError($"{LOG_PREFIX} _leaderboardButton non assigné dans l'inspecteur.");
            valid = false;
        }

        if (_leaderboardPanel == null)
        {
            Debug.LogError($"{LOG_PREFIX} _leaderboardPanel non assigné dans l'inspecteur.");
            valid = false;
        }

        return valid;
    }
}
