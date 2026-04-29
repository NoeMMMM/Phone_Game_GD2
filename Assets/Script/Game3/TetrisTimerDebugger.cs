using UnityEngine;

/// <summary>
/// Debugger temporaire pour TetrisTimerManager.
/// À supprimer après validation.
/// </summary>
public class TetrisTimerDebugger : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX    = "[TimerDebugger]";
    private const float  LOG_INTERVAL  = 5f;

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField]
    private TetrisTimerManager _timer;

    // ── État interne ──────────────────────────────────────────────────────────

    private float _timeSinceLastLog;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_timer == null)
        {
            Debug.LogError($"{LOG_PREFIX} _timer non assigné dans l'inspecteur.", this);
            return;
        }

        _timer.OnTimerTick     += HandleTimerTick;
        _timer.OnTimerFinished += HandleTimerFinished;
    }

    private void OnDestroy()
    {
        if (_timer == null) return;

        _timer.OnTimerTick     -= HandleTimerTick;
        _timer.OnTimerFinished -= HandleTimerFinished;
    }

    // ── Context Menu ──────────────────────────────────────────────────────────

    [ContextMenu("Start Timer")]
    private void StartTimer() => _timer.StartTimer();

    [ContextMenu("Stop Timer")]
    private void StopTimer() => _timer.StopTimer();

    [ContextMenu("Reset Timer")]
    private void ResetTimer() => _timer.ResetTimer();

    [ContextMenu("Log Current Time")]
    private void LogCurrentTime()
    {
        Debug.Log($"{LOG_PREFIX} {_timer.GetFormattedTime()} ({_timer.CurrentTime:F2}s) — Running: {_timer.IsRunning}");
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private void HandleTimerTick(float currentTime)
    {
        _timeSinceLastLog += Time.deltaTime;

        if (_timeSinceLastLog < LOG_INTERVAL) return;

        _timeSinceLastLog = 0f;
        Debug.Log($"{LOG_PREFIX} OnTimerTick — {_timer.GetFormattedTime()} ({currentTime:F1}s)");
    }

    private void HandleTimerFinished()
    {
        Debug.Log($"{LOG_PREFIX} *** OnTimerFinished émis — le timer a atteint 0. Victoire ! ***");
    }
}
