using System;

/// <summary>
/// Représente une entrée dans le leaderboard local.
/// Classe sérialisable pour JsonUtility — ne contient que des données brutes.
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    /// <summary>Nom du joueur.</summary>
    public string PlayerName;

    /// <summary>Temps total en secondes (le plus bas = meilleur).</summary>
    public float TotalTime;

    /// <param name="playerName">Nom du joueur.</param>
    /// <param name="totalTime">Temps total en secondes.</param>
    public LeaderboardEntry(string playerName, float totalTime)
    {
        PlayerName = playerName;
        TotalTime  = totalTime;
    }
}
