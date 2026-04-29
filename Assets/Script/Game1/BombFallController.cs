using System;
using System.Collections;
using UnityEngine;
// AJOUT : aucun using supplémentaire nécessaire

/// <summary>
/// Gère la descente d'une bombe par téléportation entre des points fixes.
/// À placer sur le prefab de bombe. Initialisé au spawn via Initialize().
/// Ne connaît ni le chevalier, ni le score, ni l'explosion.
/// MODIFIÉ : ajout de OnAnyBombReachedCatchZone et de la protection _isBeingCaught.
/// </summary>
public class BombFallController : MonoBehaviour
{
    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Durée en secondes entre chaque téléportation vers le point suivant")]
    private float _teleportInterval = 0.5f;

    // ── État interne ──────────────────────────────────────────────────────────

    private Transform[] _descentPoints;
    private int         _currentPointIndex;
    private int         _laneIndex;
    private Coroutine   _fallCoroutine;

    // AJOUT : verrou empêchant OnAnyBombReachedGround d'être émis si la bombe a déjà été attrapée
    private bool _isBeingCaught;

    // ── Events statiques publics ──────────────────────────────────────────────

    /// <summary>
    /// Émis quand une bombe atteint le dernier point de descente (le sol).
    /// Paramètre : laneIndex (0–3), pour qu'ExplosionController sache où réagir.
    /// Statique : évite de re-câbler l'event à chaque spawn/destruction.
    /// </summary>
    public static event Action<int> OnAnyBombReachedGround;

    /// <summary>
    /// AJOUT : émis quand la bombe atteint l'avant-dernier point de descente.
    /// Ouvre une fenêtre de _teleportInterval secondes pour que BombCarryController attrape la bombe.
    /// Paramètre : la bombe concernée, pour que le handler puisse appeler GetCaught() dessus.
    /// Statique : même raison que les autres events de cette classe.
    /// </summary>
    public static event Action<BombFallController> OnAnyBombReachedCatchZone;

    /// <summary>
    /// Émis quand une bombe est attrapée par le chevalier.
    /// Paramètre : la bombe concernée, pour que BombRecycler la traite.
    /// Statique : même raison que OnAnyBombReachedGround.
    /// </summary>
    public static event Action<BombFallController> OnAnyBombCaught;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Index de la ligne de descente (0–3), correspondant au soldat lanceur.</summary>
    public int LaneIndex         => _laneIndex;

    /// <summary>Index du point de descente actuellement occupé par la bombe.</summary>
    public int CurrentPointIndex => _currentPointIndex;

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Initialise la bombe avec sa ligne de descente et démarre la chute.
    /// À appeler immédiatement après le spawn par BombSpawner.
    /// Si une chute était déjà en cours (réutilisation du prefab), elle est stoppée.
    /// </summary>
    /// <param name="descentPoints">Points de téléportation de haut en bas.</param>
    /// <param name="laneIndex">Index de la ligne (0 = soldat 1, …, 3 = soldat 4).</param>
    public void Initialize(Transform[] descentPoints, int laneIndex)
    {
        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }

        _descentPoints     = descentPoints;
        _laneIndex         = laneIndex;
        _currentPointIndex = 0;

        // AJOUT : reset du verrou en cas de réutilisation du prefab
        _isBeingCaught = false;

        transform.position = _descentPoints[0].position;

        _fallCoroutine = StartCoroutine(FallRoutine());
    }

    /// <summary>
    /// Arrête la chute et signale que la bombe a été attrapée.
    /// Appelé par BombCarryController quand le chevalier attrape la bombe.
    /// </summary>
    public void GetCaught()
    {
        // AJOUT : lever le verrou avant d'arrêter la coroutine — protège la fin de FallRoutine
        _isBeingCaught = true;

        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }

        OnAnyBombCaught?.Invoke(this);
        Destroy(gameObject);
    }

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void OnDestroy()
    {
        // Sécurité : stoppe la coroutine si la bombe est détruite de l'extérieur
        if (_fallCoroutine != null)
        {
            StopCoroutine(_fallCoroutine);
            _fallCoroutine = null;
        }
    }

    // ── Coroutine de descente ─────────────────────────────────────────────────

    /// <summary>
    /// Téléporte la bombe de point en point à intervalle régulier.
    /// MODIFIÉ : émet OnAnyBombReachedCatchZone depuis l'avant-dernier point,
    /// puis vérifie _isBeingCaught avant d'émettre OnAnyBombReachedGround.
    /// </summary>
    private IEnumerator FallRoutine()
    {
        // Descend jusqu'au dernier point en téléportation pure, sans fenêtre d'attrape.
        while (_currentPointIndex < _descentPoints.Length - 1)
        {
            yield return new WaitForSeconds(_teleportInterval);

            _currentPointIndex++;
            transform.position = _descentPoints[_currentPointIndex].position;
        }

        // Dernier point atteint : ouvrir la fenêtre d'attrape.
        // Le chevalier dispose d'un interval complet pour attraper la bombe avant l'explosion.
        OnAnyBombReachedCatchZone?.Invoke(this);

        yield return new WaitForSeconds(_teleportInterval);

        // Si GetCaught() a été appelé pendant l'attente, la bombe est déjà détruite.
        if (_isBeingCaught) yield break;

        // Non rattrapée → explosion au sol
        _fallCoroutine = null;
        OnAnyBombReachedGround?.Invoke(_laneIndex);
        Destroy(gameObject);
    }
}
