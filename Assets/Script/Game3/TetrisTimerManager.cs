using System;
using UnityEngine;

/// <summary>
/// Compte à rebours local au Jeu 3 (Tetris).
/// Démarre à _initialTime et descend vers 0. À chaque ligne effacée par TetrisBoard,
/// on retire _secondsRemovedPerLine * count secondes supplémentaires.
///
/// Victoire : timer atteint 0 → OnTimerFinished émis → TetrisGameManager conclut le jeu.
/// Défaite  : gérée séparément par TetrisGameManager via TetrisBoard.OnTopReached.
///
/// Différence avec GlobalTimerManager :
///   GlobalTimerManager (singleton persistant) compte le temps total qui MONTE.
///   TetrisTimerManager (local à Game3Scene) est un compte à rebours qui DESCEND.
///   Les deux tournent en parallèle pendant le Jeu 3.
///
/// Responsabilité unique : maintenir le compte à rebours et émettre les events de tick et de fin.
/// Aucun appel à GameFlowManager — c'est TetrisGameManager qui orchestrera la victoire.
/// </summary>
public class TetrisTimerManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[TetrisTimer]";

    // ── Références inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Grille Tetris — pour s'abonner à OnLinesCleared.")]
    private TetrisBoard _board;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Temps initial du compte à rebours en secondes (défaut : 300 = 5 minutes).")]
    private float _initialTime = 300f;

    [SerializeField, Tooltip("Secondes retirées par ligne effacée (appliqué à chaque ligne, donc ×count pour un multi-line).")]
    private float _secondsRemovedPerLine = 5f;

    // ── État interne ──────────────────────────────────────────────────────────

    private float _currentTime;
    private bool  _isRunning     = false;
    private bool  _hasReachedZero = false;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Temps restant en secondes.</summary>
    public float CurrentTime     => _currentTime;

    /// <summary>Vrai si le compte à rebours est actif.</summary>
    public bool  IsRunning       => _isRunning;

    /// <summary>Vrai si le timer a atteint 0 (garde-fou : OnTimerFinished n'est émis qu'une fois).</summary>
    public bool  HasReachedZero  => _hasReachedZero;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis à chaque frame quand le timer tourne, et immédiatement après un retrait de temps par ligne.
    /// Paramètre : temps restant en secondes (pour le HUD).
    /// </summary>
    public event Action<float> OnTimerTick;

    /// <summary>
    /// Émis une seule fois quand le timer atteint 0.
    /// TetrisGameManager écoute cet event pour déclencher la victoire.
    /// </summary>
    public event Action OnTimerFinished;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (_board == null)
        {
            Debug.LogError($"{LOG_PREFIX} _board non assigné dans l'inspecteur.", this);
            return;
        }

        _currentTime = _initialTime;
        _board.OnLinesCleared += HandleLinesCleared;
    }

    private void OnDestroy()
    {
        if (_board != null)
            _board.OnLinesCleared -= HandleLinesCleared;
    }

    private void Update()
    {
        if (!_isRunning || _hasReachedZero) return;

        _currentTime -= Time.deltaTime;
        OnTimerTick?.Invoke(_currentTime);

        if (_currentTime <= 0f)
            TriggerTimerFinished();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Démarre le compte à rebours.
    /// Appelé par TetrisGameManager au début de la partie.
    /// N'a aucun effet si HasReachedZero est vrai — appeler ResetTimer() d'abord.
    /// </summary>
    public void StartTimer()
    {
        if (_hasReachedZero)
        {
            Debug.LogWarning($"{LOG_PREFIX} StartTimer() ignoré : le timer a déjà atteint 0. Appeler ResetTimer() avant de relancer.", this);
            return;
        }

        _isRunning = true;
        Debug.Log($"{LOG_PREFIX} Démarré à {_currentTime:F1} secondes.");
    }

    /// <summary>
    /// Met le compte à rebours en pause sans le réinitialiser.
    /// </summary>
    public void StopTimer()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Remet le compte à rebours à _initialTime et efface le garde-fou.
    /// Émet OnTimerTick pour rafraîchir l'UI immédiatement.
    /// </summary>
    public void ResetTimer()
    {
        _currentTime    = _initialTime;
        _hasReachedZero = false;
        _isRunning      = false;

        OnTimerTick?.Invoke(_currentTime);
    }

    /// <summary>
    /// Retourne le temps restant au format MM:SS (sans millisecondes).
    /// Exemple : 187.5f → "03:07"
    /// </summary>
    public string GetFormattedTime()
    {
        float clamped = Mathf.Max(0f, _currentTime);
        int   minutes = Mathf.FloorToInt(clamped / 60f);
        int   seconds = Mathf.FloorToInt(clamped % 60f);

        return $"{minutes:00}:{seconds:00}";
    }

    // ── Handler privé ─────────────────────────────────────────────────────────

    /// <summary>
    /// Appelé par TetrisBoard.OnLinesCleared quand des lignes sont effacées.
    /// Retire du temps proportionnellement au nombre de lignes.
    /// </summary>
    private void HandleLinesCleared(int count)
    {
        if (_hasReachedZero) return;

        float timeToRemove = count * _secondsRemovedPerLine;
        _currentTime -= timeToRemove;

        Debug.Log($"{LOG_PREFIX} {count} ligne(s) → -{timeToRemove:F1}s. Temps restant : {_currentTime:F1}s.");

        if (_currentTime <= 0f)
        {
            TriggerTimerFinished();
        }
        else
        {
            // Rafraîchissement UI immédiat sans attendre la prochaine frame.
            OnTimerTick?.Invoke(_currentTime);
        }
    }

    // ── Utilitaire privé ──────────────────────────────────────────────────────

    /// <summary>
    /// Clamp le temps à 0, arrête le timer et émet OnTimerFinished une seule fois.
    /// </summary>
    private void TriggerTimerFinished()
    {
        _currentTime    = 0f;
        _hasReachedZero = true;
        _isRunning      = false;

        OnTimerTick?.Invoke(_currentTime);

        Debug.Log($"{LOG_PREFIX} Timer atteint zéro — victoire imminente.");
        OnTimerFinished?.Invoke();
    }
}
