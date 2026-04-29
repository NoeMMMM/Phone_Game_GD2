using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Script de test temporaire pour SceneLoader — À SUPPRIMER après validation.
/// Permet de déclencher n'importe quelle transition de scène via touches clavier
/// ou via les boutons de l'inspecteur (clic droit sur la méthode > Invoke).
/// À placer sur un GameObject "_DEBUG_SceneLoader" enfant de _PersistentManagers.
/// </summary>
public class SceneLoaderDebugger : MonoBehaviour
{
    private const string LOG_PREFIX = "[SceneLoaderDebugger]";

    private void Update()
    {
        // Raccourcis clavier pour tester rapidement chaque transition en Play Mode
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.tKey.wasPressedThisFrame) LoadMainMenu();
        if (kb.yKey.wasPressedThisFrame) LoadLore();
        if (kb.uKey.wasPressedThisFrame) LoadGame1();
        if (kb.iKey.wasPressedThisFrame) LoadGame2();
        if (kb.oKey.wasPressedThisFrame) LoadGame3();
        if (kb.pKey.wasPressedThisFrame) LoadEnd();
    }

    /// <summary>Charge MainMenuScene. Touche : T.</summary>
    public void LoadMainMenu()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de MainMenu demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.MainMenu);
    }

    /// <summary>Charge LoreScene. Touche : Y.</summary>
    public void LoadLore()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de Lore demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.Lore);
    }

    /// <summary>Charge Game1Scene. Touche : U.</summary>
    public void LoadGame1()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de Game1 demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.Game1);
    }

    /// <summary>Charge Game2Scene. Touche : I.</summary>
    public void LoadGame2()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de Game2 demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.Game2);
    }

    /// <summary>Charge Game3Scene. Touche : O.</summary>
    public void LoadGame3()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de Game3 demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.Game3);
    }

    /// <summary>Charge EndScene. Touche : P.</summary>
    public void LoadEnd()
    {
        Debug.Log($"{LOG_PREFIX} Chargement de EndScreen demandé.");
        SceneLoader.Instance?.LoadScene(SceneLoader.SceneName.EndScreen);
    }
}
