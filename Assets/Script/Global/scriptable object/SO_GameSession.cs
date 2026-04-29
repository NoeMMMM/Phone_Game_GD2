using UnityEngine;

/// <summary>
/// ScriptableObject contenant l'état de la session de jeu en cours.
/// Partagé entre les scènes via une référence inspecteur.
/// Ne contient aucune logique de persistance ni de leaderboard.
/// </summary>
[CreateAssetMenu(fileName = "SO_GameSession", menuName = "Scriptable Objects/SO_GameSession")]
public class SO_GameSession : ScriptableObject
{
    [Header("État de la session — lecture seule en inspecteur")]

    [SerializeField, Tooltip("Temps total cumulé en secondes sur les 3 mini-jeux")]
    private float _totalTime;

    [SerializeField, Tooltip("Index du mini-jeu en cours (0 = Jeu1, 1 = Jeu2, 2 = Jeu3)")]
    private int _currentGameIndex;

    [SerializeField, Tooltip("Nom saisi par le joueur à la fin de la partie")]
    private string _playerName;

    // ── Propriétés publiques en lecture seule ─────────────────────────────────

    /// <summary>Temps total cumulé en secondes.</summary>
    public float TotalTime => _totalTime;

    /// <summary>Index du mini-jeu en cours (0, 1 ou 2).</summary>
    public int CurrentGameIndex => _currentGameIndex;

    /// <summary>Nom du joueur pour l'enregistrement dans le leaderboard.</summary>
    public string PlayerName => _playerName;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    /// <summary>
    /// Appelé automatiquement à chaque entrée en Play Mode dans l'éditeur,
    /// et au chargement de l'application en build.
    /// Garantit que la session repart toujours de zéro, même si les champs
    /// [SerializeField] ont été modifiés et persistés lors d'une session d'édition précédente.
    /// NOTE : GameFlowManager doit également appeler ResetSession() au clic sur Jouer,
    /// au cas où ce SO aurait été chargé avant ce OnEnable (référence persistante entre scènes).
    /// </summary>
    private void OnEnable()
    {
        ResetSession();
    }

    // ── Méthodes de mutation ──────────────────────────────────────────────────

    /// <summary>
    /// Ajoute un delta de temps au compteur cumulé.
    /// Appelé chaque frame par GlobalTimerManager lorsque le timer est actif.
    /// </summary>
    public void AddTime(float delta)
    {
        _totalTime += delta;
    }

    /// <summary>
    /// Passe au mini-jeu suivant.
    /// Appelé par GameFlowManager à chaque transition de jeu.
    /// </summary>
    public void AdvanceToNextGame()
    {
        _currentGameIndex++;
    }

    /// <summary>
    /// Définit le nom du joueur saisi en fin de partie.
    /// </summary>
    public void SetPlayerName(string playerName)
    {
        _playerName = playerName;
    }

    /// <summary>
    /// Réinitialise complètement la session pour une nouvelle partie.
    /// À appeler au lancement d'une nouvelle session depuis le menu principal.
    /// </summary>
    public void ResetSession()
    {
        _totalTime       = 0f;
        _currentGameIndex = 0;
        _playerName       = string.Empty;
    }

    /// <summary>
    /// Retourne le temps formaté en mm:ss.cc pour l'affichage UI.
    /// Exemple : "02:34.75"
    /// </summary>
    public string GetFormattedTime()
    {
        int minutes      = Mathf.FloorToInt(_totalTime / 60f);
        int seconds      = Mathf.FloorToInt(_totalTime % 60f);
        int centiseconds = Mathf.FloorToInt((_totalTime * 100f) % 100f);
        return $"{minutes:00}:{seconds:00}.{centiseconds:00}";
    }
}
