using UnityEngine;

/// <summary>
/// Script de test temporaire — À SUPPRIMER après validation de SwipeInputReader.
/// S'abonne à tous les events de SwipeInputReader et les affiche dans la console.
/// À placer sur un GameObject vide nommé "InputDebugger" dans la scène SampleScene.
/// </summary>
public class InputDebugger : MonoBehaviour
{
    // Référence stockée pour pouvoir se désabonner proprement dans OnDestroy
    private SwipeInputReader _reader;

    /// <summary>
    /// Start() garantit que tous les Awake() ont été exécutés, donc
    /// SwipeInputReader.Instance est déjà initialisé quand on s'abonne.
    /// </summary>
    private void Start()
    {
        _reader = SwipeInputReader.Instance;

        if (_reader == null)
        {
            Debug.LogError("[InputDebugger] SwipeInputReader introuvable. " +
                           "Vérifier qu'un GameObject portant ce script est présent dans la scène.");
            return;
        }

        _reader.OnSwipeLeft  += LogSwipeLeft;
        _reader.OnSwipeRight += LogSwipeRight;
        _reader.OnSwipeUp    += LogSwipeUp;
        _reader.OnSwipeDown  += LogSwipeDown;
        _reader.OnTap        += LogTap;
        _reader.OnHoldStart  += LogHoldStart;
        _reader.OnHoldEnd    += LogHoldEnd;

        Debug.Log("[InputDebugger] Abonné à tous les events de SwipeInputReader. Prêt.");
    }

    private void OnDestroy()
    {
        if (_reader == null) return;

        _reader.OnSwipeLeft  -= LogSwipeLeft;
        _reader.OnSwipeRight -= LogSwipeRight;
        _reader.OnSwipeUp    -= LogSwipeUp;
        _reader.OnSwipeDown  -= LogSwipeDown;
        _reader.OnTap        -= LogTap;
        _reader.OnHoldStart  -= LogHoldStart;
        _reader.OnHoldEnd    -= LogHoldEnd;
    }

    // ── Handlers de log ───────────────────────────────────────────────────────

    private void LogSwipeLeft()             => Debug.Log("[Input] ← Swipe Gauche");
    private void LogSwipeRight()            => Debug.Log("[Input] → Swipe Droite");
    private void LogSwipeUp()              => Debug.Log("[Input] ↑ Swipe Haut");
    private void LogSwipeDown()            => Debug.Log("[Input] ↓ Swipe Bas");
    private void LogTap(Vector2 pos)       => Debug.Log($"[Input] ● Tap @ écran {pos}");
    private void LogHoldStart(Vector2 pos) => Debug.Log($"[Input] ⬛ Hold Start @ écran {pos}");
    private void LogHoldEnd()              => Debug.Log("[Input] ⬜ Hold End");
}
