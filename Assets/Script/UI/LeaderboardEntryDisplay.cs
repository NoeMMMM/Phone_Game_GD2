using TMPro;
using UnityEngine;

/// <summary>
/// Affiche les données d'UNE entrée du leaderboard sur son prefab.
/// Responsabilité unique : recevoir rang + nom + temps via SetData() et les afficher.
/// Aucune logique de chargement ni de tri — c'est LeaderboardDisplay qui s'en occupe.
/// </summary>
public class LeaderboardEntryDisplay : MonoBehaviour
{
    // ── Références UI — inspecteur ────────────────────────────────────────────

    [SerializeField, Tooltip("Texte du rang (ex : \"#1\").")]
    private TextMeshProUGUI _rankText;

    [SerializeField, Tooltip("Texte du nom du joueur.")]
    private TextMeshProUGUI _nameText;

    [SerializeField, Tooltip("Texte du temps formaté (ex : \"03:07.542\").")]
    private TextMeshProUGUI _timeText;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Remplit les trois champs texte avec les données de l'entrée.
    /// Appelé par LeaderboardDisplay après l'instanciation du prefab.
    /// </summary>
    /// <param name="rank">Position dans le classement (1 = meilleur).</param>
    /// <param name="playerName">Nom du joueur.</param>
    /// <param name="timeInSeconds">Temps total en secondes.</param>
    public void SetData(int rank, string playerName, float timeInSeconds)
    {
        if (_rankText == null || _nameText == null || _timeText == null)
        {
            Debug.LogError("[LeaderboardEntryDisplay] Une ou plusieurs références UI sont non assignées.", this);
            return;
        }

        _rankText.text = $"#{rank}";
        _nameText.text = playerName;
        _timeText.text = TimeFormatter.Format(timeInSeconds);
    }
}
