using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôleur de l'écran de lore.
/// Récupère le contenu (titre + corps) depuis GameFlowManager au Start(),
/// l'affiche dans les champs TextMeshPro, et écoute le bouton Continuer.
/// Pas singleton, pas persistant — appartient à LoreScene.
/// </summary>
public class LoreScreenController : MonoBehaviour
{
    private const string LOG_PREFIX = "[LoreScreenController]";

    // ── Références UI ─────────────────────────────────────────────────────────

    [SerializeField, Tooltip("Texte du titre de l'écran de lore.")]
    private TextMeshProUGUI _titleText;

    [SerializeField, Tooltip("Texte du corps narratif ou tutoriel.")]
    private TextMeshProUGUI _bodyText;

    [SerializeField, Tooltip("Bouton Continuer — déclenche AdvanceFlow() sur GameFlowManager.")]
    private Button _continueButton;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        DisplayLoreContent();
        _continueButton.onClick.AddListener(HandleContinueClicked);

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.GameTransition);
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(HandleContinueClicked);
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void HandleContinueClicked()
    {
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);
        GameFlowManager.Instance?.AdvanceFlow();
    }

    // ── Logique d'affichage ───────────────────────────────────────────────────

    /// <summary>
    /// Interroge GameFlowManager et injecte le contenu dans les champs UI.
    /// Affiche un message d'erreur visible si le contenu est manquant ou si
    /// GameFlowManager n'est pas instancié (test en scène isolée).
    /// </summary>
    private void DisplayLoreContent()
    {
        if (GameFlowManager.Instance == null)
        {
            Debug.LogError($"{LOG_PREFIX} GameFlowManager.Instance est null. " +
                           "Assurez-vous de lancer depuis MainMenuScene ou d'avoir " +
                           "_PersistentManagers dans la scène de démarrage.");

            SetErrorDisplay("Erreur", "GameFlowManager introuvable.\nLancez depuis MainMenuScene.");
            return;
        }

        GameFlowManager.LoreContent content = GameFlowManager.Instance.GetCurrentLoreContent();

        if (content == null)
        {
            string state = GameFlowManager.Instance.CurrentState.ToString();
            Debug.LogError($"{LOG_PREFIX} Aucun contenu de lore pour l'état : {state}.");

            SetErrorDisplay("Erreur", $"Contenu de lore manquant pour l'état :\n{state}");
            return;
        }

        _titleText.text = content.title;
        _bodyText.text  = content.body;
    }

    /// <summary>
    /// Affiche un message d'erreur dans les champs UI pour le débogage en scène isolée.
    /// Ne bloque pas le bouton Continuer — le joueur peut continuer malgré l'erreur.
    /// </summary>
    private void SetErrorDisplay(string title, string body)
    {
        _titleText.text = title;
        _bodyText.text  = body;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que toutes les références SerializeField sont assignées.
    /// Retourne false et log une erreur par référence manquante si ce n'est pas le cas.
    /// </summary>
    private bool ValidateReferences()
    {
        bool valid = true;

        if (_titleText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _titleText non assigné dans l'inspecteur.");
            valid = false;
        }

        if (_bodyText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bodyText non assigné dans l'inspecteur.");
            valid = false;
        }

        if (_continueButton == null)
        {
            Debug.LogError($"{LOG_PREFIX} _continueButton non assigné dans l'inspecteur.");
            valid = false;
        }

        return valid;
    }
}
