using System;
using UnityEngine;

/// <summary>
/// Compte le nombre cumulé de bombes envoyées sur la porte du château.
/// Émet OnDoorDestroyed quand le seuil est atteint.
/// Responsabilité unique : comptage et signalement de la victoire du Jeu 1.
/// À placer sur un GameObject dédié (ex. "Game1Logic") dans la scène du Jeu 1.
/// </summary>
public class BombCountManager : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX = "[BombCountManager]";

    // ── Paramètres inspecteur ─────────────────────────────────────────────────

    [SerializeField, Tooltip("Référence au BombCarryController placé sur /Player")]
    private BombCarryController _bombCarryController;

    [SerializeField, Tooltip("Nombre de bombes à envoyer sur la porte pour la détruire")]
    private int _bombsRequiredToDestroyDoor = 15;

    [SerializeField, Tooltip("(Optionnel) Référence à la session de jeu — réservé pour usage futur")]
    private SO_GameSession _gameSession;

    // ── État interne ──────────────────────────────────────────────────────────

    private int  _currentBombsOnDoor;
    private bool _doorDestroyed;

    // ── Events publics ────────────────────────────────────────────────────────

    /// <summary>
    /// Émis à chaque bombe(s) envoyée(s) sur la porte.
    /// Paramètres : (current, required) — consommé par HudDisplay pour l'affichage.
    /// </summary>
    public event Action<int, int> OnBombsCountChanged;

    /// <summary>
    /// Émis une seule fois quand _bombsRequiredToDestroyDoor est atteint.
    /// Consommé par Game1Manager pour déclencher la transition vers le Jeu 2.
    /// </summary>
    public event Action OnDoorDestroyed;

    // ── Propriétés publiques ──────────────────────────────────────────────────

    /// <summary>Nombre de bombes actuellement envoyées sur la porte.</summary>
    public int CurrentBombsOnDoor => _currentBombsOnDoor;

    /// <summary>Seuil de bombes nécessaires pour détruire la porte.</summary>
    public int BombsRequiredToDestroyDoor => _bombsRequiredToDestroyDoor;

    /// <summary>Vrai quand la porte a été détruite.</summary>
    public bool IsDoorDestroyed => _doorDestroyed;

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidateDependencies()) return;

        _currentBombsOnDoor = 0;
        _doorDestroyed      = false;

        _bombCarryController.OnBombsThrown += HandleBombsThrown;
    }

    private void OnDestroy()
    {
        if (_bombCarryController != null)
            _bombCarryController.OnBombsThrown -= HandleBombsThrown;
    }

    // ── API publique ──────────────────────────────────────────────────────────

    /// <summary>
    /// Remet le compteur à zéro et déverrouille la porte.
    /// Appelée par Game1Manager si le Jeu 1 doit être relancé.
    /// </summary>
    public void ResetCounter()
    {
        _currentBombsOnDoor = 0;
        _doorDestroyed      = false;

        Debug.Log($"{LOG_PREFIX} Compteur réinitialisé.");
    }

    // ── Handler interne ───────────────────────────────────────────────────────

    /// <summary>
    /// Reçoit le nombre de bombes lancées depuis BombCarryController.OnBombsThrown.
    /// </summary>
    private void HandleBombsThrown(int count)
    {
        // Garde-fou : la porte est déjà détruite, on ignore tout lancer supplémentaire.
        if (_doorDestroyed) return;

        _currentBombsOnDoor += count;

        OnBombsCountChanged?.Invoke(_currentBombsOnDoor, _bombsRequiredToDestroyDoor);
        FeedbackManager.Instance?.PlayFeedback(FeedbackType.DoorHit);

        Debug.Log($"{LOG_PREFIX} Bombes sur la porte : {_currentBombsOnDoor}/{_bombsRequiredToDestroyDoor}");

        if (_currentBombsOnDoor >= _bombsRequiredToDestroyDoor)
            TriggerDoorDestroyed();
    }

    /// <summary>
    /// Déclenche la destruction de la porte et l'event de victoire.
    /// </summary>
    private void TriggerDoorDestroyed()
    {
        _doorDestroyed = true;

        FeedbackManager.Instance?.PlayFeedback(FeedbackType.DoorDestroyed);
        Debug.Log($"{LOG_PREFIX} Porte détruite — Jeu 1 terminé !");

        OnDoorDestroyed?.Invoke();
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateDependencies()
    {
        if (_bombCarryController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bombCarryController non assigné dans l'inspecteur.");
            return false;
        }

        if (_bombsRequiredToDestroyDoor <= 0)
        {
            Debug.LogError($"{LOG_PREFIX} _bombsRequiredToDestroyDoor doit être supérieur à 0.");
            return false;
        }

        return true;
    }
}
