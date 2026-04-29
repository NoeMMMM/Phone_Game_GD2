using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Classe C# pure gérant la lecture et l'écriture du leaderboard Top 10 en JSON local.
/// Aucune dépendance MonoBehaviour — instanciée et appelée directement
/// par EndScreenController (écriture) et LeaderboardDisplay (lecture).
/// </summary>
public class LeaderboardSaveSystem
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string NOM_FICHIER_SAUVEGARDE = "leaderboard.json";
    private const int    NOMBRE_MAX_ENTREES      = 10;

    // ── Chemin de sauvegarde ──────────────────────────────────────────────────

    /// <summary>
    /// Chemin complet vers le fichier JSON dans le dossier persistant de l'application.
    /// Sur Android : /data/data/[packageName]/files/
    /// </summary>
    private static string CheminSauvegarde =>
        Path.Combine(Application.persistentDataPath, NOM_FICHIER_SAUVEGARDE);

    // ── Wrapper de sérialisation ──────────────────────────────────────────────

    /// <summary>
    /// Conteneur nécessaire car JsonUtility ne sérialise pas
    /// les List&lt;T&gt; directement à la racine du JSON.
    /// </summary>
    [Serializable]
    private class DonneesLeaderboard
    {
        public List<LeaderboardEntry> Entrees = new List<LeaderboardEntry>();
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Charge le leaderboard depuis le fichier JSON local.
    /// Retourne une liste vide si le fichier n'existe pas ou est corrompu.
    /// </summary>
    public List<LeaderboardEntry> Load()
    {
        if (!File.Exists(CheminSauvegarde))
            return new List<LeaderboardEntry>();

        try
        {
            string json             = File.ReadAllText(CheminSauvegarde);
            DonneesLeaderboard data = JsonUtility.FromJson<DonneesLeaderboard>(json);
            return data?.Entrees ?? new List<LeaderboardEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LeaderboardSaveSystem] Échec du chargement : {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }

    /// <summary>
    /// Ajoute une nouvelle entrée, trie par temps croissant (plus bas = meilleur),
    /// conserve uniquement le Top 10, puis sauvegarde en JSON.
    /// </summary>
    /// <param name="playerName">Nom saisi par le joueur.</param>
    /// <param name="totalTime">Temps total en secondes.</param>
    public void AddEntry(string playerName, float totalTime)
    {
        List<LeaderboardEntry> entries = Load();

        entries.Add(new LeaderboardEntry(playerName, totalTime));
        entries.Sort((a, b) => a.TotalTime.CompareTo(b.TotalTime));

        // Conserver uniquement le Top 10
        if (entries.Count > NOMBRE_MAX_ENTREES)
            entries.RemoveRange(NOMBRE_MAX_ENTREES, entries.Count - NOMBRE_MAX_ENTREES);

        Save(entries);
    }

    // ── Méthode privée ────────────────────────────────────────────────────────

    /// <summary>
    /// Sérialise la liste d'entrées en JSON et l'écrit sur le disque.
    /// </summary>
    private void Save(List<LeaderboardEntry> entries)
    {
        try
        {
            DonneesLeaderboard data = new DonneesLeaderboard { Entrees = entries };
            string json             = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(CheminSauvegarde, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LeaderboardSaveSystem] Échec de la sauvegarde : {e.Message}");
        }
    }
}
