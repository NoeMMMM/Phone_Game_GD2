using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton persistant responsable du chargement asynchrone des scènes.
/// Expose un fade noir configurable entre deux scènes.
/// Ne contient aucune logique de flux de jeu — c'est le rôle de GameFlowManager.
/// À placer sur un GameObject enfant de _PersistentManagers.
/// </summary>
[DefaultExecutionOrder(-80)]
public class SceneLoader : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SceneLoader Instance { get; private set; }

    // ── Enum des scènes ───────────────────────────────────────────────────────

    /// <summary>
    /// Identifiants typés des scènes du projet.
    /// Évite les strings litterales dans le code appelant.
    /// </summary>
    public enum SceneName
    {
        MainMenu,
        Lore,
        Game1,
        Game2,
        Game3,
        EndScreen
    }

    // ── Mapping enum → nom de scène Unity ────────────────────────────────────

    /// <summary>
    /// Correspondance entre l'enum SceneName et le nom exact de la scène dans Build Settings.
    /// Modifier ici si les noms de scènes changent dans le projet.
    /// </summary>
    private static readonly Dictionary<SceneName, string> SceneNameMap = new()
    {
        { SceneName.MainMenu,  "MainMenuScene" },
        { SceneName.Lore,      "LoreScene"     },
        { SceneName.Game1,     "Game1Scene"    },
        { SceneName.Game2,     "Game2Scene"    },
        { SceneName.Game3,     "Game3Scene"    },
        { SceneName.EndScreen, "EndScene"      }
    };

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("CanvasGroup de l'image noire plein écran pour le fade. " +
                             "Si null, les transitions se font sans fade.")]
    private CanvasGroup _fadeCanvasGroup;

    [SerializeField, Tooltip("Durée en secondes du fade in et du fade out.")]
    private float _fadeDuration = 0.5f;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Garde-fou : empêche de déclencher un deuxième chargement pendant qu'un est en cours.</summary>
    private bool _isLoading;

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[SceneLoader]";

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        // Unicité du singleton — le DontDestroyOnLoad est géré par PersistentRoot sur le parent
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (_fadeCanvasGroup == null)
        {
            Debug.LogWarning($"{LOG_PREFIX} _fadeCanvasGroup non assigné — les transitions se feront sans fade.");
            return;
        }

        // Assure que l'écran est visible au démarrage
        _fadeCanvasGroup.alpha          = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Retrouve l'enum SceneName correspondant à un nom de scène Unity (string).
    /// Utilisé par OrientationManager pour déterminer l'orientation requise
    /// à partir du nom de scène reçu dans SceneManager.sceneLoaded.
    /// Retourne null si la scène n'est pas référencée dans le mapping.
    /// </summary>
    public static SceneLoader.SceneName? GetSceneNameFromUnityName(string unityName)
    {
        foreach (var kvp in SceneNameMap)
        {
            if (kvp.Value == unityName)
                return kvp.Key;
        }

        return null;
    }

    /// <summary>
    /// Charge la scène spécifiée de manière asynchrone, avec fade noir si configuré.
    /// </summary>
    /// <param name="sceneName">Scène cible (enum typé).</param>
    /// <param name="onComplete">Callback optionnel déclenché après la fin du fade in dans la nouvelle scène.</param>
    public void LoadScene(SceneName sceneName, Action onComplete = null)
    {
        if (_isLoading)
        {
            Debug.LogWarning($"{LOG_PREFIX} Chargement déjà en cours — requête ignorée ({sceneName}).");
            return;
        }

        StartCoroutine(LoadSceneRoutine(sceneName, onComplete));
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    /// <summary>
    /// Orchestre le fade out, le chargement async, puis le fade in.
    /// </summary>
    private IEnumerator LoadSceneRoutine(SceneName sceneName, Action onComplete)
    {
        _isLoading = true;

        // Fade out : écran qui noircit
        yield return StartCoroutine(FadeRoutine(0f, 1f, _fadeDuration));

        // Chargement asynchrone de la scène
        string targetSceneName = SceneNameMap[sceneName];
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);

        if (loadOperation == null)
        {
            Debug.LogError($"{LOG_PREFIX} Impossible de charger la scène '{targetSceneName}'. " +
                           "Vérifiez qu'elle est bien ajoutée dans Build Settings.");
            _isLoading = false;
            yield break;
        }

        // Attendre la fin du chargement
        while (!loadOperation.isDone)
            yield return null;

        // Fade in : écran qui s'éclaircit
        yield return StartCoroutine(FadeRoutine(1f, 0f, _fadeDuration));

        _isLoading = false;

        onComplete?.Invoke();
    }

    /// <summary>
    /// Anime l'alpha du CanvasGroup de startAlpha à endAlpha sur duration secondes.
    /// Utilise Time.unscaledDeltaTime pour fonctionner même si Time.timeScale = 0
    /// (ex : OrientationManager qui met le jeu en pause).
    /// Si _fadeCanvasGroup est null, retourne immédiatement sans fade.
    /// </summary>
    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration)
    {
        if (_fadeCanvasGroup == null) yield break;

        // Bloque les raycasts pendant le fade out (écran noir = joueur ne doit pas interagir)
        _fadeCanvasGroup.blocksRaycasts = endAlpha > startAlpha;

        float elapsed = 0f;
        _fadeCanvasGroup.alpha = startAlpha;

        while (elapsed < duration)
        {
            elapsed                += Time.unscaledDeltaTime;
            _fadeCanvasGroup.alpha  = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = endAlpha;

        // Libère les raycasts une fois l'écran revenu transparent
        _fadeCanvasGroup.blocksRaycasts = endAlpha >= 1f;
    }
}
