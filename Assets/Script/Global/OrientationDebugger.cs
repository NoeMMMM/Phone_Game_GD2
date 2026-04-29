using UnityEngine;

/// <summary>
/// Script de test temporaire pour OrientationManager — À SUPPRIMER après validation.
/// S'abonne à OnOrientationMismatchChanged et logue chaque changement d'état dans la console.
/// À placer sur n'importe quel GameObject persistant (ex : _PersistentManagers lui-même,
/// ou un _DEBUG_Orientation dédié).
/// </summary>
public class OrientationDebugger : MonoBehaviour
{
    private const string LOG_PREFIX = "[OrientationDebugger]";

    private void OnEnable()
    {
        if (OrientationManager.Instance != null)
            OrientationManager.Instance.OnOrientationMismatchChanged += HandleOrientationChanged;
        else
            Debug.LogWarning($"{LOG_PREFIX} OrientationManager.Instance introuvable au OnEnable.");
    }

    private void OnDisable()
    {
        if (OrientationManager.Instance != null)
            OrientationManager.Instance.OnOrientationMismatchChanged -= HandleOrientationChanged;
    }

    private void HandleOrientationChanged(bool isMismatch)
    {
        if (isMismatch)
            Debug.Log($"{LOG_PREFIX} Mauvaise orientation détectée — overlay affiché, jeu en pause.");
        else
            Debug.Log($"{LOG_PREFIX} Orientation correcte — overlay masqué, jeu repris.");
    }
}
