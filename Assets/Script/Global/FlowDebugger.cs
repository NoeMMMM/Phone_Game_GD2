using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script de test temporaire pour GameFlowManager — À SUPPRIMER après validation.
/// Permet de déclencher StartNewGame() et AdvanceFlow() via touches clavier
/// ou via les boutons de l'inspecteur.
/// À placer sur un GameObject "_DEBUG_Flow" dans la scène MainMenuScene.
/// </summary>
public class FlowDebugger : MonoBehaviour
{
    private const string LOG_PREFIX = "[FlowDebugger]";

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // N = nouvelle partie (StartNewGame)
        if (kb.nKey.wasPressedThisFrame) StartNewGame();

        // Espace = avancer dans le flux (AdvanceFlow)
        if (kb.spaceKey.wasPressedThisFrame) AdvanceFlow();

        // M = retour au menu (GoToMainMenu)
        if (kb.mKey.wasPressedThisFrame) GoToMainMenu();

        // L = logger le contenu lore courant
        if (kb.lKey.wasPressedThisFrame) LogCurrentLoreContent();
    }

    /// <summary>Démarre une nouvelle partie. Touche : N.</summary>
    public void StartNewGame()
    {
        Debug.Log($"{LOG_PREFIX} StartNewGame() demandé.");
        GameFlowManager.Instance?.StartNewGame();
    }

    /// <summary>Avance dans le flux. Touche : Espace.</summary>
    public void AdvanceFlow()
    {
        string state = GameFlowManager.Instance != null
            ? GameFlowManager.Instance.CurrentState.ToString()
            : "N/A";

        Debug.Log($"{LOG_PREFIX} AdvanceFlow() demandé depuis l'état : {state}.");
        GameFlowManager.Instance?.AdvanceFlow();
    }

    /// <summary>Retourne au menu principal. Touche : M.</summary>
    public void GoToMainMenu()
    {
        Debug.Log($"{LOG_PREFIX} GoToMainMenu() demandé.");
        GameFlowManager.Instance?.GoToMainMenu();
    }

    /// <summary>Logue le contenu de lore de l'état courant. Touche : L.</summary>
    public void LogCurrentLoreContent()
    {
        GameFlowManager.LoreContent content = GameFlowManager.Instance?.GetCurrentLoreContent();

        if (content != null)
            Debug.Log($"{LOG_PREFIX} Contenu lore courant — Titre : \"{content.title}\" | Body : \"{content.body}\"");
        else
            Debug.LogWarning($"{LOG_PREFIX} Aucun contenu lore pour l'état courant.");
    }
}
