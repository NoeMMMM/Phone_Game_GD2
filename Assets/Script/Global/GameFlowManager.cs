using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton persistant qui orchestre le flux global du jeu.
/// Responsabilité unique : connaître l'état courant du flux, décider quelle scène
/// charger ensuite, et fournir le contenu textuel des écrans de lore.
/// Ne contient aucune logique de gameplay.
/// À placer sur un GameObject enfant de _PersistentManagers.
/// </summary>
[DefaultExecutionOrder(-70)]
public class GameFlowManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameFlowManager Instance { get; private set; }

    // ── Enum d'état du flux ───────────────────────────────────────────────────

    /// <summary>
    /// Représente l'étape courante du joueur dans le flux global du jeu.
    /// Flux complet :
    /// MainMenu → Lore1_Story → Lore1_Tutorial → Game1
    ///          → Lore2_Story → Lore2_Tutorial → Game2
    ///          → Lore3_Story → Lore3_Tutorial → Game3
    ///          → EndScreen → MainMenu
    /// </summary>
    public enum FlowState
    {
        MainMenu,
        Lore1_Story,
        Lore1_Tutorial,
        Game1,
        Lore2_Story,
        Lore2_Tutorial,
        Game2,
        Lore3_Story,
        Lore3_Tutorial,
        Game3,
        EndScreen
    }

    // ── Classe interne de contenu de lore ─────────────────────────────────────

    /// <summary>
    /// Contenu textuel affiché sur un écran de lore spécifique.
    /// Rempli dans l'inspecteur — 6 entrées au total (3 story + 3 tutorial).
    /// </summary>
    [Serializable]
    public class LoreContent
    {
        [Tooltip("État du flux auquel ce contenu correspond.")]
        public FlowState forState;

        [Tooltip("Titre affiché en haut de l'écran de lore.")]
        [TextArea(3, 10)]
        public string title;

        [Tooltip("Corps du texte narratif ou tutoriel.")]
        [TextArea(5, 20)]
        public string body;
    }

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("ScriptableObject partagé contenant l'état de la session.")]
    private SO_GameSession _gameSession;

    [SerializeField, Tooltip("6 entrées de contenu de lore (Lore1_Story, Lore1_Tutorial, " +
                             "Lore2_Story, Lore2_Tutorial, Lore3_Story, Lore3_Tutorial).")]
    private List<LoreContent> _loreContents = new();

    // ── État interne ──────────────────────────────────────────────────────────

    private FlowState _currentState = FlowState.MainMenu;

    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[GameFlowManager]";

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>État courant du flux, en lecture seule.</summary>
    public FlowState CurrentState => _currentState;

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

        if (_gameSession == null)
            Debug.LogError($"{LOG_PREFIX} _gameSession non assigné dans l'inspecteur.");
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appelée par MainMenuController au clic sur "Jouer".
    /// Réinitialise la session et démarre le flux depuis le premier écran de lore.
    /// </summary>
    public void StartNewGame()
    {
        _gameSession?.ResetSession();
        GlobalTimerManager.Instance?.ResetTimer();

        _currentState = FlowState.Lore1_Story;

        Debug.Log($"{LOG_PREFIX} Nouvelle partie démarrée. État : {_currentState}.");

        SceneLoader.Instance.LoadScene(SceneLoader.SceneName.Lore);
    }

    /// <summary>
    /// Appelée par EndScreenController pour retourner au menu principal.
    /// </summary>
    public void GoToMainMenu()
    {
        _currentState = FlowState.MainMenu;

        Debug.Log($"{LOG_PREFIX} Retour au menu principal.");

        SceneLoader.Instance.LoadScene(SceneLoader.SceneName.MainMenu);
    }

    /// <summary>
    /// Fait avancer le flux vers l'étape suivante.
    /// Appelée par LoreScreenController (bouton "Continuer") et par les GameManagers
    /// (condition de victoire de chaque mini-jeu).
    /// </summary>
    public void AdvanceFlow()
    {
        switch (_currentState)
        {
            // ── Depuis le menu principal ──────────────────────────────────────
            case FlowState.MainMenu:
                TransitionTo(FlowState.Lore1_Story, SceneLoader.SceneName.Lore);
                break;

            // ── Jeu 1 ─────────────────────────────────────────────────────────
            case FlowState.Lore1_Story:
                // Reste sur la scène Lore — LoreScreenController recharge son contenu
                TransitionTo(FlowState.Lore1_Tutorial, SceneLoader.SceneName.Lore);
                break;

            case FlowState.Lore1_Tutorial:
                // Démarre le timer après le chargement de la scène (pas pendant le fade)
                TransitionTo(FlowState.Game1, SceneLoader.SceneName.Game1,
                    () => GlobalTimerManager.Instance?.StartTimer());
                break;

            case FlowState.Game1:
                TransitionTo(FlowState.Lore2_Story, SceneLoader.SceneName.Lore);
                break;

            // ── Jeu 2 ─────────────────────────────────────────────────────────
            case FlowState.Lore2_Story:
                TransitionTo(FlowState.Lore2_Tutorial, SceneLoader.SceneName.Lore);
                break;

            case FlowState.Lore2_Tutorial:
                TransitionTo(FlowState.Game2, SceneLoader.SceneName.Game2);
                break;

            case FlowState.Game2:
                TransitionTo(FlowState.Lore3_Story, SceneLoader.SceneName.Lore);
                break;

            // ── Jeu 3 ─────────────────────────────────────────────────────────
            case FlowState.Lore3_Story:
                TransitionTo(FlowState.Lore3_Tutorial, SceneLoader.SceneName.Lore);
                break;

            case FlowState.Lore3_Tutorial:
                TransitionTo(FlowState.Game3, SceneLoader.SceneName.Game3);
                break;

            case FlowState.Game3:
                // Arrête le timer avant le chargement : le temps final est figé dans SO_GameSession
                GlobalTimerManager.Instance?.StopTimer();
                TransitionTo(FlowState.EndScreen, SceneLoader.SceneName.EndScreen);
                break;

            case FlowState.EndScreen:
                // L'EndScreen gère lui-même le retour via GoToMainMenu()
                Debug.LogWarning($"{LOG_PREFIX} AdvanceFlow() appelé depuis EndScreen — utiliser GoToMainMenu() à la place.");
                break;

            default:
                Debug.LogWarning($"{LOG_PREFIX} AdvanceFlow() appelé depuis un état non géré : {_currentState}.");
                break;
        }
    }

    // ── Méthodes utilitaires publiques ────────────────────────────────────────

    /// <summary>
    /// Retourne le contenu de lore correspondant à l'état courant.
    /// Retourne null si aucun contenu n'est configuré pour cet état.
    /// </summary>
    public LoreContent GetCurrentLoreContent()
    {
        LoreContent content = _loreContents.Find(lc => lc.forState == _currentState);

        if (content == null)
            Debug.LogWarning($"{LOG_PREFIX} Aucun contenu de lore pour l'état {_currentState}.");

        return content;
    }

    /// <summary>
    /// Retourne true si l'état courant est un écran de lore.
    /// Utilisé par LoreScreenController pour valider son contexte à l'entrée de la scène.
    /// </summary>
    public bool IsCurrentStateLore()
    {
        return _currentState.ToString().Contains("Lore");
    }

    // ── Méthode privée de transition ──────────────────────────────────────────

    /// <summary>
    /// Met à jour l'état interne et demande le chargement de la scène cible.
    /// </summary>
    private void TransitionTo(FlowState nextState, SceneLoader.SceneName targetScene, Action onComplete = null)
    {
        _currentState = nextState;

        Debug.Log($"{LOG_PREFIX} Transition → {_currentState} (scène : {targetScene}).");

        SceneLoader.Instance.LoadScene(targetScene, onComplete);
    }
}
