using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrateur du Jeu 1 (Game &amp; Watch).
/// Responsabilité unique : démarrer les systèmes au lancement de la scène
/// et déclencher la transition vers le Jeu 2 quand la porte est détruite.
/// Ne contient aucune logique de gameplay (bombes, chevalier, score).
/// À placer sur un GameObject dédié "Game1Manager" dans la scène du Jeu 1.
/// </summary>
public class Game1Manager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[Game1Manager]";

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("BombSpawner de la scène — gère l'apparition des bombes")]
    private BombSpawner _bombSpawner;

    [SerializeField, Tooltip("BombCountManager de la scène — émet OnDoorDestroyed à la victoire")]
    private BombCountManager _bombCountManager;

    [SerializeField, Tooltip("(Optionnel) Référence explicite au GlobalTimerManager. " +
                             "Si null, le script utilisera GlobalTimerManager.Instance.")]
    private GlobalTimerManager _timerManager;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Délai en secondes entre la destruction de la porte et la transition vers le Jeu 2. " +
                             "Laisse le temps aux feedbacks audio/visuels de se terminer.")]
    private float _delayBeforeTransition = 2f;

    // MODIFIÉ : commentaire mis à jour — dans le flux normal, GameFlowManager démarre
    // le timer avant d'entrer dans Game1. Ce flag reste utile uniquement pour les
    // tests en isolation (chargement direct de Game1Scene sans passer par le menu).
    [SerializeField, Tooltip("Si vrai, démarre le timer global au Start() s'il n'est pas déjà en cours. " +
                             "Dans le flux normal, GameFlowManager a déjà démarré le timer en amont. " +
                             "Utile uniquement pour tester le Jeu 1 en isolation directe.")]
    private bool _autoStartTimerIfNotRunning = true;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Garde-fou : empêche de déclencher la transition deux fois.</summary>
    private bool _game1Completed;

    // ── Event public ──────────────────────────────────────────────────────────

    /// <summary>
    /// Émis une seule fois quand le Jeu 1 est terminé, après le délai de transition.
    /// Sera consommé par GameFlowManager pour charger la scène suivante.
    /// </summary>
    public event Action OnGame1Completed;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateDependencies()) return;

        // Résolution de la référence au timer si non câblée dans l'inspecteur
        if (_timerManager == null)
            _timerManager = GlobalTimerManager.Instance;

        if (_timerManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} GlobalTimerManager introuvable — ni assigné en inspecteur ni disponible via Instance.");
            return;
        }

        // Démarre le timer global si nécessaire (mode test en isolation)
        if (_autoStartTimerIfNotRunning && _timerManager.GetCurrentTime() == 0f)
        {
            _timerManager.StartTimer();
            Debug.Log($"{LOG_PREFIX} Timer global démarré par Game1Manager (_autoStartTimerIfNotRunning = true).");
        }

        // Démarre le spawn de bombes
        _bombSpawner.StartSpawning();

        // Écoute la condition de victoire du Jeu 1
        _bombCountManager.OnDoorDestroyed += HandleDoorDestroyed;

        Debug.Log($"{LOG_PREFIX} Démarré — BombSpawner actif, en attente de victoire.");
    }

    private void OnDestroy()
    {
        // Désabonnement propre pour éviter les fuites mémoire si la scène est déchargée
        if (_bombCountManager != null)
            _bombCountManager.OnDoorDestroyed -= HandleDoorDestroyed;
    }

    // ── Handler victoire ──────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par BombCountManager.OnDoorDestroyed quand les 15 bombes ont atteint la porte.
    /// </summary>
    private void HandleDoorDestroyed()
    {
        // Garde-fou : ne pas déclencher la transition deux fois
        if (_game1Completed) return;

        _game1Completed = true;

        _bombSpawner.StopSpawning();

        Debug.Log($"{LOG_PREFIX} Porte détruite ! Jeu 1 gagné. Transition dans {_delayBeforeTransition}s.");

        StartCoroutine(TransitionToGame2Routine());
    }

    // ── Coroutine de transition ───────────────────────────────────────────────

    /// <summary>
    /// Attend le délai configuré, joue le feedback de transition,
    /// émet OnGame1Completed, puis délègue à GameFlowManager.AdvanceFlow()
    /// pour charger la scène suivante dans le flux global.
    /// </summary>
    private IEnumerator TransitionToGame2Routine()
    {
        yield return new WaitForSeconds(_delayBeforeTransition);

        // Feedback audio/visuel de transition (hook — non bloquant si non assigné)
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.GameTransition);

        // MODIFIÉ : émission de l'event avant AdvanceFlow pour que les abonnés locaux
        // (HUD, feedbacks) puissent réagir avant le changement de scène.
        OnGame1Completed?.Invoke();

        // MODIFIÉ : remplacement du Debug.Log placeholder par l'appel réel à GameFlowManager.
        // Garde-fou pour les tests en isolation (Game1Scene chargée directement sans le menu).
        if (GameFlowManager.Instance == null)
        {
            string elapsedTime = _timerManager != null ? _timerManager.GetFormattedTime() : "N/A";
            Debug.LogError($"{LOG_PREFIX} GameFlowManager.Instance est null — impossible d'avancer dans le flux. " +
                           $"Temps écoulé : {elapsedTime}. Lance le jeu depuis MainMenuScene pour le flux complet.");
            yield break;
        }

        GameFlowManager.Instance.AdvanceFlow();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que toutes les références obligatoires sont assignées.
    /// Retourne false si l'une d'elles manque — le Start() s'interrompt proprement.
    /// </summary>
    private bool ValidateDependencies()
    {
        bool isValid = true;

        if (_bombSpawner == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bombSpawner non assigné dans l'inspecteur.");
            isValid = false;
        }

        if (_bombCountManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bombCountManager non assigné dans l'inspecteur.");
            isValid = false;
        }

        return isValid;
    }
}
