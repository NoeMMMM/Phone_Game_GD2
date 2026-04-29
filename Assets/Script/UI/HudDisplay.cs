using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Affiche le HUD du Jeu 1 : timer global, icônes de bombes qui suivent le chevalier,
/// barre de durabilité de la porte.
/// Entièrement piloté par events — aucun Update().
/// Responsabilité unique : réception d'events et mise à jour des éléments UI.
/// À placer sur le Canvas du Jeu 1 (Screen Space - Overlay).
/// </summary>
public class HudDisplay : MonoBehaviour
{
    // ── Constantes ────────────────────────────────────────────────────────────

    private const string LOG_PREFIX      = "[HudDisplay]";
    private const int    BOMB_ICON_COUNT = 3;

    // ── Références UI ─────────────────────────────────────────────────────────

    [Header("Références UI")]
    [SerializeField, Tooltip("Texte du timer global en haut à gauche")]
    private TextMeshProUGUI _timerText;

    [SerializeField, Tooltip("3 icônes de bombes : 0 = gauche, 1 = dessus, 2 = droite")]
    private Image[] _bombIcons;

    [SerializeField, Tooltip("Barre de durabilité de la porte (Max = 1, se vide à mesure des impacts)")]
    private Slider _doorDurabilitySlider;

    [SerializeField, Tooltip("(Optionnel) Texte 'X/Y' au-dessus de la barre de durabilité")]
    private TextMeshProUGUI _doorDurabilityText;

    // ── Références managers et chevalier ──────────────────────────────────────

    [Header("Managers")]
    [SerializeField, Tooltip("GlobalTimerManager — source du timer cumulé")]
    private GlobalTimerManager _timerManager;

    [SerializeField, Tooltip("BombCarryController sur /Player — source du nombre de bombes portées")]
    private BombCarryController _bombCarryController;

    [SerializeField, Tooltip("BombCountManager — source du compteur de bombes sur la porte")]
    private BombCountManager _bombCountManager;

    [SerializeField, Tooltip("KnightPositionController sur /Player — pour suivre la position du chevalier")]
    private KnightPositionController _knightController;

    [SerializeField, Tooltip("Caméra de jeu pour la conversion monde → écran. Si null, Camera.main est utilisée.")]
    private Camera _gameCamera;

    // ── Visuels et offsets ────────────────────────────────────────────────────

    [Header("Visuels")]
    [SerializeField, Tooltip("Couleur d'une icône de bombe active (bombe portée)")]
    private Color _bombIconActiveColor = Color.white;

    [SerializeField, Tooltip("Couleur d'une icône de bombe inactive (emplacement vide)")]
    private Color _bombIconInactiveColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Offsets icônes (pixels écran depuis le centre du chevalier)")]
    [SerializeField, Tooltip("Offset de l'icône gauche")]
    private Vector2 _bombIconOffsetLeft  = new Vector2(-50f, 0f);

    [SerializeField, Tooltip("Offset de l'icône du dessus")]
    private Vector2 _bombIconOffsetTop   = new Vector2(0f, 80f);

    [SerializeField, Tooltip("Offset de l'icône droite")]
    private Vector2 _bombIconOffsetRight = new Vector2(50f, 0f);

    // ── Cycle de vie Unity ────────────────────────────────────────────────────

    private void Start()
    {
        // Résolution de GlobalTimerManager via Instance si non câblé en inspecteur.
        // Nécessaire car GlobalTimerManager vit sur _PersistentManagers (DontDestroyOnLoad)
        // et n'est pas visible dans l'inspecteur de Game1Scene.
        if (_timerManager == null)
            _timerManager = GlobalTimerManager.Instance;

        if (!ValidateDependencies()) return;

        SubscribeToEvents();
        InitializeDisplay();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ── Abonnements ───────────────────────────────────────────────────────────

    private void SubscribeToEvents()
    {
        _timerManager.OnTimerTick               += HandleTimerTick;
        _bombCarryController.OnBombCountChanged += HandleBombCountChanged;
        _bombCountManager.OnBombsCountChanged   += HandleDoorBombsChanged;
        _knightController.OnPositionChanged     += HandleKnightPositionChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (_timerManager != null)
            _timerManager.OnTimerTick               -= HandleTimerTick;

        if (_bombCarryController != null)
            _bombCarryController.OnBombCountChanged -= HandleBombCountChanged;

        if (_bombCountManager != null)
            _bombCountManager.OnBombsCountChanged   -= HandleDoorBombsChanged;

        if (_knightController != null)
            _knightController.OnPositionChanged     -= HandleKnightPositionChanged;
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    private void InitializeDisplay()
    {
        if (_timerText != null)
            _timerText.text = "00:00.000";

        SetAllBombIcons(0);

        if (_doorDurabilitySlider != null)
            _doorDurabilitySlider.value = 1f;

        if (_doorDurabilityText != null)
        {
            int required = _bombCountManager.BombsRequiredToDestroyDoor;
            _doorDurabilityText.text = $"{required}/{required}";
        }

        // Position initiale des icônes autour du chevalier
        UpdateBombIconsPosition();
    }

    // ── Handlers d'events ─────────────────────────────────────────────────────

    /// <summary>
    /// Reçoit le tick du timer depuis GlobalTimerManager.OnTimerTick.
    /// </summary>
    private void HandleTimerTick(float currentTime)
    {
        if (_timerText == null) return;

        _timerText.text = _timerManager.GetFormattedTime();
    }

    /// <summary>
    /// Reçoit le nombre de bombes portées depuis BombCarryController.OnBombCountChanged.
    /// </summary>
    private void HandleBombCountChanged(int currentBombs)
    {
        SetAllBombIcons(currentBombs);
    }

    /// <summary>
    /// Reçoit le compteur de bombes sur la porte depuis BombCountManager.OnBombsCountChanged.
    /// </summary>
    private void HandleDoorBombsChanged(int current, int required)
    {
        if (_doorDurabilitySlider != null)
            _doorDurabilitySlider.value = 1f - ((float)current / required);

        if (_doorDurabilityText != null)
            _doorDurabilityText.text = $"{required - current}/{required}";
    }

    /// <summary>
    /// Reçoit la nouvelle position du chevalier depuis KnightPositionController.OnPositionChanged.
    /// </summary>
    private void HandleKnightPositionChanged(int newIndex)
    {
        UpdateBombIconsPosition();
    }

    // ── Positionnement des icônes ─────────────────────────────────────────────

    /// <summary>
    /// Convertit la position monde du chevalier en position écran et applique
    /// les offsets sur chaque RectTransform d'icône.
    /// </summary>
    private void UpdateBombIconsPosition()
    {
        if (_bombIcons == null || _knightController == null || _gameCamera == null) return;

        Vector3 knightWorldPos  = _knightController.transform.position;
        Vector2 knightScreenPos = _gameCamera.WorldToScreenPoint(knightWorldPos);

        Vector2[] offsets = new Vector2[BOMB_ICON_COUNT]
        {
            _bombIconOffsetLeft,
            _bombIconOffsetTop,
            _bombIconOffsetRight
        };

        for (int i = 0; i < _bombIcons.Length; i++)
        {
            if (_bombIcons[i] == null) continue;

            RectTransform rt = _bombIcons[i].rectTransform;
            rt.position = new Vector3(
                knightScreenPos.x + offsets[i].x,
                knightScreenPos.y + offsets[i].y,
                0f
            );
        }
    }

    // ── Utilitaires ───────────────────────────────────────────────────────────

    /// <summary>
    /// Met à jour la couleur de toutes les icônes selon le nombre de bombes portées.
    /// </summary>
    private void SetAllBombIcons(int activeBombs)
    {
        if (_bombIcons == null) return;

        if (_bombIcons.Length != BOMB_ICON_COUNT)
        {
            Debug.LogWarning($"{LOG_PREFIX} _bombIcons contient {_bombIcons.Length} entrée(s) " +
                             $"au lieu de {BOMB_ICON_COUNT}. L'affichage peut être incorrect.");
        }

        for (int i = 0; i < _bombIcons.Length; i++)
        {
            if (_bombIcons[i] == null) continue;

            _bombIcons[i].color = i < activeBombs ? _bombIconActiveColor : _bombIconInactiveColor;
        }
    }

    // ── Validation ────────────────────────────────────────────────────────────

    private bool ValidateDependencies()
    {
        bool valid = true;

        if (_timerText == null)
        {
            Debug.LogError($"{LOG_PREFIX} _timerText non assigné.");
            valid = false;
        }

        if (_bombIcons == null || _bombIcons.Length == 0)
        {
            Debug.LogError($"{LOG_PREFIX} _bombIcons non assigné ou vide.");
            valid = false;
        }

        if (_doorDurabilitySlider == null)
        {
            Debug.LogError($"{LOG_PREFIX} _doorDurabilitySlider non assigné.");
            valid = false;
        }

        if (_timerManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} _timerManager non assigné.");
            valid = false;
        }

        if (_bombCarryController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bombCarryController non assigné.");
            valid = false;
        }

        if (_bombCountManager == null)
        {
            Debug.LogError($"{LOG_PREFIX} _bombCountManager non assigné.");
            valid = false;
        }

        if (_knightController == null)
        {
            Debug.LogError($"{LOG_PREFIX} _knightController non assigné.");
            valid = false;
        }

        // Résolution Camera.main en fallback
        if (_gameCamera == null)
        {
            _gameCamera = Camera.main;

            if (_gameCamera == null)
            {
                Debug.LogError($"{LOG_PREFIX} Aucune caméra trouvée (ni assignée ni Camera.main).");
                valid = false;
            }
            else
            {
                Debug.LogWarning($"{LOG_PREFIX} _gameCamera non assigné — Camera.main utilisée en fallback.");
            }
        }

        // Avertissement si le Canvas n'est pas en Screen Space - Overlay
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning($"{LOG_PREFIX} Le Canvas n'est pas en Screen Space - Overlay. " +
                             "Le positionnement par WorldToScreenPoint peut être incorrect.");
        }

        return valid;
    }
}
