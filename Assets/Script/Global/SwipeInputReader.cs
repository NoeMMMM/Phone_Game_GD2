using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Singleton persistant responsable de la lecture des inputs tactiles.
/// Détecte swipes (4 directions), taps et holds via EnhancedTouch.
/// Expose uniquement des C# events — ne contient aucune logique de jeu.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SwipeInputReader : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static SwipeInputReader Instance { get; private set; }

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Distance minimale en pixels écran pour qualifier un mouvement de swipe")]
    private float _minSwipeDistance = 50f;

    [SerializeField, Tooltip("Durée en secondes sans mouvement significatif pour déclencher un hold")]
    private float _holdThreshold = 0.2f;

    // ── Events publics ────────────────────────────────────────────────────────

    public event Action            OnSwipeLeft;
    public event Action            OnSwipeRight;
    public event Action            OnSwipeUp;
    public event Action            OnSwipeDown;

    /// <summary>Tap détecté. Paramètre : position en pixels écran.</summary>
    public event Action<Vector2>   OnTap;

    /// <summary>
    /// Hold déclenché après <see cref="_holdThreshold"/> secondes sans mouvement significatif.
    /// Paramètre : position de départ du contact en pixels écran.
    /// </summary>
    public event Action<Vector2>   OnHoldStart;

    /// <summary>Fin du hold — doigt levé après qu'un OnHoldStart a été émis.</summary>
    public event Action            OnHoldEnd;

    // ── État interne ──────────────────────────────────────────────────────────

    /// <summary>Position à laquelle le premier doigt a touché l'écran.</summary>
    private Vector2   _startPosition;

    /// <summary>Dernière position connue du premier doigt (mise à jour dans HandleFingerMove).</summary>
    private Vector2   _currentPosition;

    /// <summary>Indique qu'un OnHoldStart a été émis et attend un OnHoldEnd.</summary>
    private bool      _holdStartFired;

    /// <summary>Référence à la coroutine de détection du hold, pour pouvoir l'annuler.</summary>
    private Coroutine _holdCoroutine;

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
    }

    private void OnEnable()
    {
        // Doit être activé avant d'accéder à Touch.onFinger*
        EnhancedTouchSupport.Enable();

        Touch.onFingerDown += HandleFingerDown;
        Touch.onFingerMove += HandleFingerMove;
        Touch.onFingerUp   += HandleFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= HandleFingerDown;
        Touch.onFingerMove -= HandleFingerMove;
        Touch.onFingerUp   -= HandleFingerUp;

        EnhancedTouchSupport.Disable();
    }

    // ── Gestion des contacts ──────────────────────────────────────────────────

    /// <summary>
    /// Enregistre la position de départ et lance la détection de hold.
    /// On ne suit que le premier doigt (index 0).
    /// </summary>
    private void HandleFingerDown(Finger finger)
    {
        if (finger.index != 0) return;

        _startPosition   = finger.screenPosition;
        _currentPosition = finger.screenPosition;
        _holdStartFired  = false;

        if (_holdCoroutine != null)
            StopCoroutine(_holdCoroutine);

        _holdCoroutine = StartCoroutine(HoldDetectionRoutine());
    }

    /// <summary>
    /// Met à jour la position courante.
    /// Annule la détection de hold si le doigt s'est trop déplacé avant la fin du seuil.
    /// </summary>
    private void HandleFingerMove(Finger finger)
    {
        if (finger.index != 0) return;

        _currentPosition = finger.screenPosition;

        // Annuler le hold uniquement si OnHoldStart n'a pas encore été émis
        if (_holdStartFired || _holdCoroutine == null) return;

        float distance = Vector2.Distance(_currentPosition, _startPosition);
        if (distance >= _minSwipeDistance)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }
    }

    /// <summary>
    /// Au lever du doigt :
    /// - Si un hold était actif → émet OnHoldEnd.
    /// - Si le delta est insuffisant → émet OnTap.
    /// - Sinon → émet le swipe dans la direction dominante.
    /// </summary>
    private void HandleFingerUp(Finger finger)
    {
        if (finger.index != 0) return;

        // Arrêter la coroutine si elle tourne encore
        if (_holdCoroutine != null)
        {
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }

        // Fin d'un hold en cours
        if (_holdStartFired)
        {
            _holdStartFired = false;
            OnHoldEnd?.Invoke();
            return;
        }

        // Analyse du geste
        Vector2 delta    = finger.screenPosition - _startPosition;
        float   distance = delta.magnitude;

        if (distance < _minSwipeDistance)
        {
            // Mouvement insuffisant → tap
            OnTap?.Invoke(finger.screenPosition);
        }
        else
        {
            // Direction dominante : horizontal ou vertical
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                if (delta.x > 0f) OnSwipeRight?.Invoke();
                else              OnSwipeLeft?.Invoke();
            }
            else
            {
                if (delta.y > 0f) OnSwipeUp?.Invoke();
                else              OnSwipeDown?.Invoke();
            }
        }
    }

    // ── Coroutine de détection hold ───────────────────────────────────────────

    /// <summary>
    /// Attend <see cref="_holdThreshold"/> secondes puis vérifie que le doigt
    /// n'a pas bougé significativement. Si c'est le cas, émet OnHoldStart.
    /// </summary>
    private IEnumerator HoldDetectionRoutine()
    {
        yield return new WaitForSeconds(_holdThreshold);

        float distance = Vector2.Distance(_currentPosition, _startPosition);
        if (distance < _minSwipeDistance)
        {
            _holdStartFired = true;
            OnHoldStart?.Invoke(_startPosition);
        }

        _holdCoroutine = null;
    }

    // ── Méthodes publiques pour les boutons UI ────────────────────────────────

    /// <summary>
    /// Simule un swipe gauche.
    /// À câbler sur le UnityEvent OnClick du bouton UI gauche dans l'inspecteur.
    /// </summary>
    public void TriggerSwipeLeft()  => OnSwipeLeft?.Invoke();

    /// <summary>
    /// Simule un swipe droite.
    /// À câbler sur le UnityEvent OnClick du bouton UI droit dans l'inspecteur.
    /// </summary>
    public void TriggerSwipeRight() => OnSwipeRight?.Invoke();
}
