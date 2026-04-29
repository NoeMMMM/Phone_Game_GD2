using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôleur de l'écran de fin de partie (EndScene).
/// Responsabilité unique : afficher le temps final, capturer le nom du joueur,
/// sauvegarder l'entrée dans le leaderboard local, et retourner au menu principal.
/// 
/// Prérequis : GlobalTimerManager.StopTimer() a déjà été appelé par GameFlowManager
/// lors de la transition Game3 → EndScreen. Le temps figé est dans SO_GameSession.TotalTime.
/// </summary>
public class EndScreenController : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[EndScreen]";

    // ── Références UI — inspecteur ────────────────────────────────────────────

    [Header("Références UI")]

    [SerializeField, Tooltip("Titre affiché en haut de l'écran de fin.")]
    private TextMeshProUGUI _titleText;

    [SerializeField, Tooltip("Texte affichant le temps total formaté.")]
    private TextMeshProUGUI _timeText;

    [SerializeField, Tooltip("Champ de saisie du nom du joueur.")]
    private TMP_InputField _nameInputField;

    [SerializeField, Tooltip("Bouton de sauvegarde du score et retour au menu.")]
    private Button _validateButton;

    [SerializeField, Tooltip("Bouton de retour au menu sans sauvegarde (optionnel — peut rester null).")]
    private Button _skipButton;

    // ── Paramètres — inspecteur ───────────────────────────────────────────────

    [Header("Paramètres")]

    [SerializeField, Tooltip("ScriptableObject partagé contenant le temps total de la session.")]
    private SO_GameSession _gameSession;

    [SerializeField, Tooltip("Nombre de caractères maximum autorisés dans le champ de saisie.")]
    private int _maxNameLength = 12;

    [SerializeField, Tooltip("Nom utilisé si le joueur valide avec un champ vide.")]
    private string _defaultName = "Anonyme";

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>
    /// Garde-fou contre la double-sauvegarde si le joueur clique plusieurs fois
    /// avant la fin du fade de transition.
    /// </summary>
    private bool _hasSaved;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateReferences()) return;

        DisplayFinalTime();
        ConfigureInputField();
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        // Retrait propre des listeners pour éviter des fuites mémoire
        // si le GameObject est détruit avant que le garbage collector ne passe.
        if (_validateButton != null)
            _validateButton.onClick.RemoveListener(HandleValidateClicked);

        if (_skipButton != null)
            _skipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que toutes les références obligatoires sont assignées dans l'inspecteur.
    /// Retourne false et logue les erreurs si une référence manque.
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_titleText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _titleText non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_timeText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _timeText non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_nameInputField == null)
        {
            Debug.LogError($"{LOG_PREFIX} _nameInputField non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_validateButton == null)
        {
            Debug.LogError($"{LOG_PREFIX} _validateButton non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_gameSession == null)
        {
            Debug.LogError($"{LOG_PREFIX} _gameSession non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Affiche le temps final dans _timeText.
    /// Utilise GlobalTimerManager.GetFormattedTime() (MM:SS.mmm) pour la cohérence
    /// avec le format HUD des mini-jeux, avec fallback sur SO_GameSession si le manager
    /// est indisponible.
    /// </summary>
    private void DisplayFinalTime()
    {
        string formattedTime = GlobalTimerManager.Instance != null
            ? GlobalTimerManager.Instance.GetFormattedTime()
            : _gameSession.GetFormattedTime();

        _timeText.text = $"Temps : {formattedTime}";

        Debug.Log($"{LOG_PREFIX} Temps final affiché : {formattedTime}");
    }

    /// <summary>
    /// Configure le TMP_InputField : limite de caractères et texte vide au démarrage.
    /// </summary>
    private void ConfigureInputField()
    {
        _nameInputField.characterLimit = _maxNameLength;
        _nameInputField.text           = string.Empty;
    }

    /// <summary>
    /// Câble les onClick des boutons en code plutôt que via l'inspecteur,
    /// pour garantir que les handlers sont toujours connectés indépendamment
    /// du câblage scène.
    /// </summary>
    private void RegisterButtonListeners()
    {
        _validateButton.onClick.AddListener(HandleValidateClicked);

        if (_skipButton != null)
            _skipButton.onClick.AddListener(HandleSkipClicked);
    }

    // ── Handlers boutons ──────────────────────────────────────────────────────

    /// <summary>
    /// Appelé au clic sur "Enregistrer mon score".
    /// Sauvegarde l'entrée dans le leaderboard local et retourne au menu principal.
    /// Le garde-fou _hasSaved empêche la double-sauvegarde en cas de clics rapides.
    /// </summary>
    private void HandleValidateClicked()
    {
        if (_hasSaved) return;
        _hasSaved = true;

        string playerName = _nameInputField.text.Trim();
        if (string.IsNullOrEmpty(playerName))
            playerName = _defaultName;

        float finalTime = _gameSession.TotalTime;

        LeaderboardSaveSystem saveSystem = new LeaderboardSaveSystem();
        saveSystem.AddEntry(playerName, finalTime);

        string formattedTime = GlobalTimerManager.Instance != null
            ? GlobalTimerManager.Instance.GetFormattedTime()
            : _gameSession.GetFormattedTime();

        Debug.Log($"{LOG_PREFIX} Score enregistré : {playerName} - {formattedTime}");

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);

        GameFlowManager.Instance.GoToMainMenu();
    }

    /// <summary>
    /// Appelé au clic sur "Retour sans sauvegarder".
    /// Retourne au menu principal sans écrire dans le leaderboard.
    /// </summary>
    private void HandleSkipClicked()
    {
        if (_hasSaved) return;

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);

        GameFlowManager.Instance.GoToMainMenu();
    }
}
