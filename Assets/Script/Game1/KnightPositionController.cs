using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gère le déplacement du chevalier entre 5 positions fixes dans le Jeu 1 (Game & Watch).
/// Réagit aux swipes via SwipeInputReader. Gère l'inversion temporaire des contrôles.
/// Ne contient aucune logique de bombes, de score ou de porte.
/// </summary>
public class KnightPositionController : MonoBehaviour
{
    // ── Constantes ─────────────────────────────────────────────────────────────

    private const int POSITION_COUNT  = 5;
    private const int CENTER_INDEX    = 2;
    private const int MIN_INDEX       = 0;
    private const int MAX_INDEX       = POSITION_COUNT - 1;

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("5 Transforms correspondant aux positions fixes du chevalier (gauche → droite)")]
    private Transform[] _positions;

    [SerializeField, Tooltip("Index de départ du chevalier (2 = centre devant la porte)")]
    private int _startingIndex = CENTER_INDEX;

    // ── État interne ──────────────────────────────────────────────────────────

    private int       _currentIndex;
    private bool      _controlsInverted;
    private Coroutine _invertCoroutine;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis après chaque déplacement réussi.
    /// Paramètre : nouvel index courant (0–4).
    /// Consommé par BombCarryController pour détecter la position centre.
    /// </summary>
    public event Action<int> OnPositionChanged;

    /// <summary>
    /// Émis quand le joueur swipe vers le haut.
    /// Consommé par BombCarryController pour déclencher le lancer de bombe.
    /// </summary>
    public event Action OnSwipeUp;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Index de position courant du chevalier (0 = extrême gauche, 4 = extrême droite).</summary>
    public int  CurrentIndex => _currentIndex;

    /// <summary>Vrai si le chevalier est à la position centrale (index 2), devant la porte.</summary>
    public bool IsAtCenter   => _currentIndex == CENTER_INDEX;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidatePositions()) return;

        _currentIndex = _startingIndex;
        transform.position = _positions[_currentIndex].position;

        SubscribeToInputs();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInputs();
    }

    // ── Abonnements SwipeInputReader ──────────────────────────────────────────

    private void SubscribeToInputs()
    {
        if (SwipeInputReader.Instance == null)
        {
            Debug.LogError("[KnightPositionController] SwipeInputReader introuvable.");
            return;
        }

        SwipeInputReader.Instance.OnSwipeLeft  += HandleSwipeLeft;
        SwipeInputReader.Instance.OnSwipeRight += HandleSwipeRight;
        SwipeInputReader.Instance.OnSwipeUp    += HandleSwipeUp;
    }

    private void UnsubscribeFromInputs()
    {
        if (SwipeInputReader.Instance == null) return;

        SwipeInputReader.Instance.OnSwipeLeft  -= HandleSwipeLeft;
        SwipeInputReader.Instance.OnSwipeRight -= HandleSwipeRight;
        SwipeInputReader.Instance.OnSwipeUp    -= HandleSwipeUp;
    }

    // ── Handlers de swipe ─────────────────────────────────────────────────────

    /// <summary>Swipe gauche → déplace vers index-1, ou index+1 si contrôles inversés.</summary>
    private void HandleSwipeLeft()
    {
        MoveTo(_controlsInverted ? _currentIndex + 1 : _currentIndex - 1);
    }

    /// <summary>Swipe droite → déplace vers index+1, ou index-1 si contrôles inversés.</summary>
    private void HandleSwipeRight()
    {
        MoveTo(_controlsInverted ? _currentIndex - 1 : _currentIndex + 1);
    }

    /// <summary>
    /// Swipe haut → ce script ne sait pas ce que ça fait.
    /// Émet OnSwipeUp pour que BombCarryController réagisse.
    /// </summary>
    private void HandleSwipeUp()
    {
        OnSwipeUp?.Invoke();
    }

    // ── Déplacement ───────────────────────────────────────────────────────────

    /// <summary>
    /// Déplace le chevalier vers newIndex (clampé entre 0 et 4).
    /// Met à jour la position Transform, émet OnPositionChanged et joue un feedback.
    /// </summary>
    private void MoveTo(int newIndex)
    {
        int clampedIndex = Mathf.Clamp(newIndex, MIN_INDEX, MAX_INDEX);

        // Ignorer si déjà à la limite dans la direction demandée
        if (clampedIndex == _currentIndex) return;

        _currentIndex          = clampedIndex;
        transform.position     = _positions[_currentIndex].position;

        OnPositionChanged?.Invoke(_currentIndex);
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.MenuClick);
    }

    // ── Inversion des contrôles ───────────────────────────────────────────────

    /// <summary>
    /// Inverse les contrôles gauche↔droite pendant <paramref name="duration"/> secondes.
    /// Si une inversion était déjà en cours, elle est réinitialisée à la nouvelle durée.
    /// Appelé par BombFallController quand une bombe explose près du chevalier.
    /// </summary>
    /// <param name="duration">Durée de l'inversion en secondes.</param>
    public void SetControlsInverted(float duration)
    {
        if (_invertCoroutine != null)
            StopCoroutine(_invertCoroutine);

        _invertCoroutine = StartCoroutine(InvertControlsRoutine(duration));
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.KnightDisoriented);
    }

    private IEnumerator InvertControlsRoutine(float duration)
    {
        _controlsInverted = true;
        yield return new WaitForSeconds(duration);
        _controlsInverted = false;
        _invertCoroutine  = null;
    }

    // ── Validation ────────────────────────────────────────────────────────────

    /// <summary>
    /// Vérifie que le tableau _positions est correctement rempli dans l'inspecteur.
    /// </summary>
    private bool ValidatePositions()
    {
        if (_positions == null || _positions.Length != POSITION_COUNT)
        {
            Debug.LogError($"[KnightPositionController] _positions doit contenir exactement " +
                           $"{POSITION_COUNT} Transforms. Actuel : {_positions?.Length ?? 0}.");
            return false;
        }

        for (int i = 0; i < _positions.Length; i++)
        {
            if (_positions[i] == null)
            {
                Debug.LogError($"[KnightPositionController] _positions[{i}] est null.");
                return false;
            }
        }

        if (_startingIndex < MIN_INDEX || _startingIndex > MAX_INDEX)
        {
            Debug.LogError($"[KnightPositionController] _startingIndex ({_startingIndex}) hors plage [0–4]. Remis à {CENTER_INDEX}.");
            _startingIndex = CENTER_INDEX;
        }

        return true;
    }
}
