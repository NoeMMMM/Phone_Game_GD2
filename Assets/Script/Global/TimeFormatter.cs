using UnityEngine;

/// <summary>
/// Utilitaire statique de formatage du temps.
/// Source unique de vérité pour le format MM:SS.mmm utilisé dans tout le projet.
/// Appelé par GlobalTimerManager, LeaderboardEntryDisplay, et tout script futur
/// ayant besoin d'afficher un temps en secondes.
/// </summary>
public static class TimeFormatter
{
    /// <summary>
    /// Convertit un temps en secondes vers le format "MM:SS.mmm".
    /// Exemple : 187.542f → "03:07.542"
    /// </summary>
    /// <param name="totalSeconds">Temps total en secondes (valeur brute).</param>
    public static string Format(float totalSeconds)
    {
        totalSeconds = Mathf.Max(0f, totalSeconds);

        int minutes      = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds      = Mathf.FloorToInt(totalSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((totalSeconds * 1000f) % 1000f);

        return $"{minutes:00}:{seconds:00}.{milliseconds:000}";
    }
}
