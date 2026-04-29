using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pilote l'affichage du panel leaderboard dans MainMenuScene.
/// Responsabilité unique : charger les entrées via LeaderboardSaveSystem à chaque
/// activation du panel, instancier les prefabs d'entrée, et gérer le bouton Fermer.
/// 
/// Ce script est attaché sur le GameObject _leaderboardPanel lui-même.
/// OnEnable() se déclenche automatiquement à chaque SetActive(true) depuis MainMenuController.
/// </summary>
public class LeaderboardDisplay : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[LeaderboardDisplay]";

    // ── Références — inspecteur ───────────────────────────────────────────────

    [Header("Références")]

    [SerializeField, Tooltip("Prefab d'une entrée individuelle (doit avoir un LeaderboardEntryDisplay).")]
    private GameObject _entryPrefab;

    [SerializeField, Tooltip("Transform parent où instancier les entrées (VerticalLayoutGroup recommandé).")]
    private Transform _entriesContainer;

    [SerializeField, Tooltip("Bouton de fermeture du panel.")]
    private Button _closeButton;

    [SerializeField, Tooltip("Message affiché si le leaderboard est vide (optionnel — peut rester null).")]
    private TextMeshProUGUI _emptyMessageText;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>
    /// Entrées instanciées lors de l'affichage courant.
    /// Détruites et reconstruites à chaque nouvelle ouverture du panel.
    /// </summary>
    private readonly List<GameObject> _spawnedEntries = new();

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        // Câblage du bouton fait une seule fois dans Start(),
        // indépendamment du nombre d'activations/désactivations du panel.
        if (_closeButton != null)
            _closeButton.onClick.AddListener(HandleCloseClicked);
        else
            Debug.LogError($"{LOG_PREFIX} _closeButton non assigné dans l'inspecteur.", this);
    }

    /// <summary>
    /// Appelé automatiquement à chaque SetActive(true) sur ce GameObject.
    /// Recharge et reconstruit la liste à chaque ouverture pour refléter
    /// les nouvelles entrées ajoutées depuis la dernière consultation.
    /// </summary>
    private void OnEnable()
    {
        if (!ValidateReferences()) return;

        ClearSpawnedEntries();
        LoadAndDisplay();
    }

    private void OnDestroy()
    {
        if (_closeButton != null)
            _closeButton.onClick.RemoveListener(HandleCloseClicked);
    }

    // ── Logique d'affichage ───────────────────────────────────────────────────

    /// <summary>
    /// Détruit tous les GameObjects d'entrée instanciés lors de l'affichage précédent.
    /// </summary>
    private void ClearSpawnedEntries()
    {
        foreach (GameObject entry in _spawnedEntries)
        {
            if (entry != null)
                Destroy(entry);
        }

        _spawnedEntries.Clear();
    }

    /// <summary>
    /// Charge les entrées depuis le JSON local et instancie un prefab par entrée.
    /// Affiche le message vide si le leaderboard ne contient aucune entrée.
    /// </summary>
    private void LoadAndDisplay()
    {
        LeaderboardSaveSystem saveSystem    = new LeaderboardSaveSystem();
        List<LeaderboardEntry> entries      = saveSystem.Load();

        // Cas leaderboard vide
        if (entries.Count == 0)
        {
            if (_emptyMessageText != null)
                _emptyMessageText.gameObject.SetActive(true);

            Debug.Log($"{LOG_PREFIX} Aucune entrée dans le leaderboard.");
            return;
        }

        // Leaderboard non vide — masquer le message vide si présent
        if (_emptyMessageText != null)
            _emptyMessageText.gameObject.SetActive(false);

        // Instanciation d'un prefab par entrée
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry    = entries[i];
            GameObject newEntry       = Instantiate(_entryPrefab, _entriesContainer);
            _spawnedEntries.Add(newEntry);

            LeaderboardEntryDisplay display = newEntry.GetComponent<LeaderboardEntryDisplay>();

            if (display == null)
            {
                Debug.LogError($"{LOG_PREFIX} Le prefab _entryPrefab n'a pas de composant " +
                               "LeaderboardEntryDisplay. Vérifie le prefab.", this);
                continue;
            }

            // Rang 1-based : index 0 = #1
            display.SetData(i + 1, entry.PlayerName, entry.TotalTime);
        }

        Debug.Log($"{LOG_PREFIX} {entries.Count} entrée(s) affichée(s).");
    }

    // ── Handler bouton ────────────────────────────────────────────────────────

    /// <summary>
    /// Ferme le panel en le désactivant.
    /// Comme ce script est sur le panel lui-même, SetActive(false) déclenche OnDisable()
    /// mais PAS OnEnable() — les entrées seront reconstruites à la prochaine ouverture.
    /// </summary>
    private void HandleCloseClicked()
    {
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);
        gameObject.SetActive(false);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie les références obligatoires. Retourne false si l'une manque.
    /// </summary>
    private bool ValidateReferences()
    {
        bool isValid = true;

        if (_entryPrefab == null)
        {
            Debug.LogError($"{LOG_PREFIX} _entryPrefab non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        if (_entriesContainer == null)
        {
            Debug.LogError($"{LOG_PREFIX} _entriesContainer non assigné dans l'inspecteur.", this);
            isValid = false;
        }

        return isValid;
    }
}
