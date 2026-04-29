using System;
using System.Collections.Generic;
using UnityEngine;

// ── Enum public — accessible depuis tous les scripts du projet ────────────────

/// <summary>
/// Identifie chaque type de feedback du projet.
/// Extensible : ajouter une valeur ici suffit pour l'exposer dans l'inspecteur.
/// </summary>
public enum FeedbackType
{
    // -- Jeu 1 : Game & Watch --------------------------------------------------
    BombCaught,
    BombMissed,
    BombThrown,
    BombExploded,
    DoorHit,
    DoorDestroyed,
    KnightDisoriented,

    // -- Jeu 2 : Snake ---------------------------------------------------------
    SnakeMove,
    SnakeEat,
    SnakeDeath,
    SnakeWin,

    // -- Jeu 3 : Tetris --------------------------------------------------------
    TetrisPieceLocked,
    TetrisLineCleared,
    TetrisGameOver,
    TetrisWin,
    TetrisRotate,

    // -- UI --------------------------------------------------------------------
    MenuClick,
    GameStart,
    GameTransition
}

// ── Classe de données sérialisable ────────────────────────────────────────────

/// <summary>
/// Association entre un FeedbackType, un AudioClip et un prefab VFX.
/// Tous les champs sont optionnels : un entry avec tout à null génère un Debug.Log.
/// </summary>
[Serializable]
public class FeedbackEntry
{
    public FeedbackType type;

    [Tooltip("Son à jouer (optionnel — peut rester null pendant le développement)")]
    public AudioClip audioClip;

    [Tooltip("Prefab d'effet visuel à instancier (optionnel)")]
    public GameObject vfxPrefab;

    [Range(0f, 1f), Tooltip("Volume de lecture du son")]
    public float volume = 1f;
}

// ── Manager principal ─────────────────────────────────────────────────────────

/// <summary>
/// Singleton persistant centralisant tous les feedbacks audio et visuels du projet.
/// Les scripts de gameplay appellent PlayFeedback(type) sans connaître
/// les assets sous-jacents — ajout de sons/VFX sans toucher à la logique de jeu.
/// </summary>
[DefaultExecutionOrder(-90)]
public class FeedbackManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static FeedbackManager Instance { get; private set; }

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Liste des feedbacks assignables. Peut rester vide pendant le développement.")]
    private List<FeedbackEntry> _feedbacks = new List<FeedbackEntry>();

    [SerializeField, Tooltip("Durée de vie en secondes des prefabs VFX instanciés avant destruction automatique")]
    private float _vfxLifetime = 3f;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>AudioSource auto-créé dans Awake pour la lecture des sons.</summary>
    private AudioSource _audioSource;

    /// <summary>
    /// Dictionnaire construit depuis _feedbacks pour une recherche en O(1).
    /// Construit une seule fois dans Awake.
    /// </summary>
    private Dictionary<FeedbackType, FeedbackEntry> _feedbackMap;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad est géré par PersistentRoot sur le parent _PersistentManagers.
        // Ne pas l'appeler ici — Unity sortirait cet enfant du parent et casserait la hiérarchie.

        InitAudioSource();
        BuildFeedbackMap();
    }

    /// <summary>
    /// Récupère ou crée l'AudioSource sur ce GameObject.
    /// </summary>
    private void InitAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // On gère le volume par clip via PlayOneShot — pas de lecture automatique
        _audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Convertit la liste sérialisée en dictionnaire pour les lookups à l'exécution.
    /// En cas de doublons dans l'inspecteur, la première entrée gagne.
    /// </summary>
    private void BuildFeedbackMap()
    {
        _feedbackMap = new Dictionary<FeedbackType, FeedbackEntry>(_feedbacks.Count);
        foreach (FeedbackEntry entry in _feedbacks)
        {
            if (!_feedbackMap.ContainsKey(entry.type))
                _feedbackMap[entry.type] = entry;
            else
                Debug.LogWarning($"[FeedbackManager] Doublon détecté dans _feedbacks pour le type : {entry.type}. La première entrée est conservée.");
        }
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Déclenche le feedback audio et/ou visuel associé au type donné.
    /// Si aucune entry n'est configurée ou si tous ses champs sont null,
    /// un Debug.Log est émis — comportement intentionnel pour le développement progressif.
    /// </summary>
    /// <param name="type">Type de feedback à déclencher.</param>
    /// <param name="position">Position monde où instancier le VFX (ignorée si pas de prefab).</param>
    public void PlayFeedback(FeedbackType type, Vector3 position = default)
    {
        if (!_feedbackMap.TryGetValue(type, out FeedbackEntry entry) ||
            (entry.audioClip == null && entry.vfxPrefab == null))
        {
            Debug.Log($"[FeedbackManager] Feedback non assigné : {type}");
            return;
        }

        // Lecture audio — PlayOneShot permet la superposition de sons identiques
        if (entry.audioClip != null)
            _audioSource.PlayOneShot(entry.audioClip, entry.volume);

        // Instanciation VFX — destruction automatique après _vfxLifetime secondes
        if (entry.vfxPrefab != null)
        {
            GameObject vfx = Instantiate(entry.vfxPrefab, position, Quaternion.identity);
            Destroy(vfx, _vfxLifetime);
        }
    }
}
