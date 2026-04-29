using System;
using UnityEngine;

/// <summary>
/// Singleton persistant responsable du chronomètre global cumulé sur les 3 mini-jeux.
/// Délègue le stockage du temps à SO_GameSession via AddTime().
/// Ne contient aucun affichage UI ni aucune logique de gameplay.
/// S'initialise après SwipeInputReader (ordre -100) mais avant les scripts de gameplay.
/// </summary>
[DefaultExecutionOrder(-90)]
public class GlobalTimerManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GlobalTimerManager Instance { get; private set; }

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("ScriptableObject partagé contenant le temps total de la session en cours")]
    private SO_GameSession _gameSession;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Indique si le chronomètre est actuellement en train de tourner.</summary>
    private bool _isRunning;

    // ── Event public ──────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenché à chaque Update lorsque le timer est actif.
    /// Paramètre : temps total écoulé en secondes (valeur brute de SO_GameSession.TotalTime).
    /// Permet la mise à jour du HUD sans couplage direct entre ce script et l'UI.
    /// </summary>
    public event Action<float> OnTimerTick;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        // Garantit l'unicité du singleton entre les scènes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad est géré par PersistentRoot sur le parent _PersistentManagers.
        // Ne pas l'appeler ici — Unity sortirait cet enfant du parent et casserait la hiérarchie.

        if (_gameSession == null)
            Debug.LogError("[GlobalTimerManager] SO_GameSession non assigné dans l'inspecteur.");
    }

    private void Update()
    {
        if (!_isRunning || _gameSession == null) return;

        _gameSession.AddTime(Time.deltaTime);
        OnTimerTick?.Invoke(_gameSession.TotalTime);
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Démarre le chronomètre.
    /// À appeler depuis GameFlowManager au début du Jeu 1.
    /// </summary>
    public void StartTimer()
    {
        _isRunning = true;
    }

    /// <summary>
    /// Met le chronomètre en pause sans réinitialiser le temps.
    /// À appeler entre deux mini-jeux (écran de lore) ou en cas de pause.
    /// </summary>
    public void PauseTimer()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Reprend le chronomètre après un appel à PauseTimer().
    /// </summary>
    public void ResumeTimer()
    {
        _isRunning = true;
    }

    /// <summary>
    /// Arrête définitivement le chronomètre.
    /// À appeler depuis GameFlowManager à la fin du Jeu 3, avant l'écran de fin.
    /// </summary>
    public void StopTimer()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Arrête le chronomètre et remet la session à zéro via SO_GameSession.ResetSession().
    /// Effet de bord intentionnel : remet également CurrentGameIndex et PlayerName à zéro.
    /// À appeler depuis GameFlowManager au clic sur le bouton Jouer du menu principal.
    /// </summary>
    public void ResetTimer()
    {
        _isRunning = false;

        if (_gameSession != null)
            _gameSession.ResetSession();
    }

    /// <summary>
    /// Retourne le temps total écoulé en secondes (valeur brute).
    /// </summary>
    public float GetCurrentTime()
    {
        return _gameSession != null ? _gameSession.TotalTime : 0f;
    }

    /// <summary>
    /// Retourne le temps formaté en MM:SS.mmm (millisecondes).
    /// Exemple : "02:34.750"
    /// Délègue à TimeFormatter pour centraliser la logique de formatage.
    /// </summary>
    public string GetFormattedTime()
    {
        float time = _gameSession != null ? _gameSession.TotalTime : 0f;
        return TimeFormatter.Format(time);
    }
}
