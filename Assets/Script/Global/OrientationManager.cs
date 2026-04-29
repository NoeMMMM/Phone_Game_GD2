using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistant qui surveille l'orientation du téléphone.
/// Si l'orientation ne correspond pas à celle attendue pour la scène courante,
/// affiche un overlay "Tourne ton téléphone" et met le jeu en pause via Time.timeScale.
/// Dès que l'orientation est correcte, l'overlay est masqué et la pause levée.
/// À placer sur un GameObject enfant de _PersistentManagers.
/// </summary>
[DefaultExecutionOrder(-60)]
public class OrientationManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static OrientationManager Instance { get; private set; }

    // ── Enum d'orientation ────────────────────────────────────────────────────

    public enum RequiredOrientation { Landscape, Portrait }

    // ── Mapping scène → orientation requise ──────────────────────────────────

    /// <summary>
    /// Orientation attendue pour chaque scène du jeu.
    /// Doit rester cohérent avec les orientations configurées dans Player Settings.
    /// </summary>
    private static readonly Dictionary<SceneLoader.SceneName, RequiredOrientation> SceneOrientationMap = new()
    {
        { SceneLoader.SceneName.MainMenu,  RequiredOrientation.Landscape },
        { SceneLoader.SceneName.Lore,      RequiredOrientation.Landscape },
        { SceneLoader.SceneName.Game1,     RequiredOrientation.Landscape },
        { SceneLoader.SceneName.Game2,     RequiredOrientation.Landscape },
        { SceneLoader.SceneName.Game3,     RequiredOrientation.Portrait  },
        { SceneLoader.SceneName.EndScreen, RequiredOrientation.Portrait  }
    };

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Panel plein écran affiché quand l'orientation est incorrecte. " +
                             "Seul ce Panel est activé/désactivé — son Canvas parent reste actif.")]
    private GameObject _rotatePhonePanel;

    [SerializeField, Tooltip("Si vrai, Time.timeScale passe à 0 quand l'orientation est incorrecte.")]
    private bool _pauseGameWhenWrongOrientation = true;

    // ── État interne ──────────────────────────────────────────────────────────

    private RequiredOrientation _currentRequiredOrientation = RequiredOrientation.Landscape;
    private bool _isInWrongOrientation;

    /// <summary>
    /// Sauvegarde du timeScale avant la pause, pour pouvoir le restaurer correctement
    /// même si un autre système a modifié la valeur (ex : menu de pause).
    /// </summary>
    private float _previousTimeScale = 1f;

    // ── Event public ──────────────────────────────────────────────────────────

    /// <summary>
    /// Émis quand l'état d'orientation change.
    /// true = mauvaise orientation (overlay affiché), false = orientation correcte.
    /// Utilisé par OrientationDebugger et tout autre système intéressé.
    /// </summary>
    public event Action<bool> OnOrientationMismatchChanged;

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[OrientationManager]";

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad est géré par PersistentRoot sur le parent _PersistentManagers.

        if (_rotatePhonePanel == null)
            Debug.LogWarning($"{LOG_PREFIX} _rotatePhonePanel non assigné — l'overlay ne s'affichera pas.");
    }

    private void Start()
    {
        // S'abonner aux changements de scène pour mettre à jour l'orientation requise
        SceneManager.sceneLoaded += HandleSceneLoaded;

        // Check immédiat sur la scène de démarrage
        UpdateRequiredOrientationForScene(SceneManager.GetActiveScene().name);
        CheckOrientation();

        // S'assurer que le panel est dans l'état correct au démarrage
        SetPanelActive(_isInWrongOrientation);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        CheckOrientation();
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par SceneManager à chaque chargement de scène.
    /// Met à jour l'orientation requise selon la scène entrante.
    /// </summary>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateRequiredOrientationForScene(scene.name);
        CheckOrientation();
    }

    // ── Logique d'orientation ─────────────────────────────────────────────────

    /// <summary>
    /// Met à jour _currentRequiredOrientation en consultant le SceneOrientationMap.
    /// Si la scène n'est pas dans le mapping (ex : scènes de dev), garde l'orientation courante.
    /// </summary>
    private void UpdateRequiredOrientationForScene(string sceneName)
    {
        SceneLoader.SceneName? sceneKey = SceneLoader.GetSceneNameFromUnityName(sceneName);

        if (sceneKey.HasValue && SceneOrientationMap.TryGetValue(sceneKey.Value, out RequiredOrientation required))
        {
            _currentRequiredOrientation = required;
            Debug.Log($"{LOG_PREFIX} Scène '{sceneName}' → orientation requise : {_currentRequiredOrientation}.");
        }
        else
        {
            Debug.Log($"{LOG_PREFIX} Scène '{sceneName}' non référencée dans le mapping — " +
                      $"orientation inchangée ({_currentRequiredOrientation}).");
        }
    }

    /// <summary>
    /// Détecte l'orientation courante et applique l'overlay + pause si nécessaire.
    /// Utilise le ratio largeur/hauteur — plus fiable que Screen.orientation sur certains devices.
    /// </summary>
    private void CheckOrientation()
    {
        RequiredOrientation currentOrientation = Screen.width > Screen.height
            ? RequiredOrientation.Landscape
            : RequiredOrientation.Portrait;

        bool mismatch = currentOrientation != _currentRequiredOrientation;

        // Activation de l'overlay
        if (mismatch && !_isInWrongOrientation)
        {
            _isInWrongOrientation = true;

            if (_pauseGameWhenWrongOrientation)
            {
                _previousTimeScale  = Time.timeScale;
                Time.timeScale      = 0f;
            }

            SetPanelActive(true);
            OnOrientationMismatchChanged?.Invoke(true);
        }
        // Désactivation de l'overlay
        else if (!mismatch && _isInWrongOrientation)
        {
            _isInWrongOrientation = false;

            if (_pauseGameWhenWrongOrientation)
                Time.timeScale = _previousTimeScale;

            SetPanelActive(false);
            OnOrientationMismatchChanged?.Invoke(false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetPanelActive(bool active)
    {
        if (_rotatePhonePanel != null)
            _rotatePhonePanel.SetActive(active);
    }
}
