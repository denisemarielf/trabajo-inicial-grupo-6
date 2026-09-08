using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

public enum MatchResultReason
{
    None = -1,
    SurviveTime = 0,        // Victoria: Sobrevivieron el tiempo limite
    DefeatedAllEnemies = 1, // Victoria: Mataron a todos los enemigos
    OutOfLives = 2,         // Derrota: Se quedaron sin vidas
    TowerDestroyed = 3      // Derrota: La torre fue destruida
}

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    // Eventos desacoplados para que otros sistemas puedan suscribirse
    public static event Action OnGameOverShown;
    public static event Action OnReturnToMenuRequested;
    public static event Action OnPlayAgainRequested;

    [Header("Paneles Principales")]
    [Tooltip("El panel contenedor general de la pantalla de fin de partida")]
    [SerializeField] private GameObject rootPanel;

    [Header("Textos de Resultado")]
    [SerializeField] private TMP_Text badgeText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text killsValueText;

    [Header("Estilos Visuales")]
    [SerializeField] private string victoryTitle = "VICTORIA";
    [SerializeField] private Color victoryColor = new Color(0.2f, 0.95f, 0.5f, 1f);

    [SerializeField] private string defeatTitle = "DERROTA";
    [SerializeField] private Color defeatColor = new Color(1.0f, 0.28f, 0.28f, 1f);

    [Header("Mensajes de Victoria")]
    [SerializeField] private string victorySurviveSubtitle = "¡Han resistido todo el tiempo limite defendiendo la base!";
    [SerializeField] private string victoryAllEnemiesSubtitle = "¡Han eliminado a todas las oleadas de enemigos!";

    [Header("Mensajes de Derrota")]
    [SerializeField] private string defeatOutOfLivesSubtitle = "Todos los defensores han caido en combate.";
    [SerializeField] private string defeatTowerDestroyedSubtitle = "La torre de defensa ha sido destruida.";

    [Header("Botones")]
    [SerializeField] private Button btnPlayAgain;
    [SerializeField] private Button btnMainMenu;

    [Header("Configuracion de Escenas")]
    [Tooltip("Nombre de la escena del menu principal para volver")]
    [SerializeField] private string mainMenuSceneName = "menuPrincipal";

    private bool isGameOverActive = false;
    private Image accentImage;
    private RectTransform cardRect;
    private CanvasGroup rootCanvasGroup;

    private Coroutine animCoroutine;
    private Coroutine tickerCoroutine;

    public static void EnsureEventSystemExists()
    {
        var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
            if (!existing.enabled) existing.enabled = true;
            return;
        }

        GameObject esGo = new GameObject("EventSystem");
        esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
        var inputModule = esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
#else
        esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        Debug.Log("[GameOverUI] EventSystem creado e inicializado con exito.");
    }

    public static GameOverUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "menuPrincipal") return null;

        EnsureEventSystemExists();

        GameObject canvasGo = GameObject.Find("GameOverCanvas");
        Canvas canvas = null;
        if (canvasGo == null)
        {
            canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            scaler.matchWidthOrHeight = 0.5f;
        }
        else
        {
            canvas = canvasGo.GetComponent<Canvas>();
        }

        GameObject go = new GameObject("GameOverUI", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rtGo = go.GetComponent<RectTransform>();
        rtGo.anchorMin = Vector2.zero;
        rtGo.anchorMax = Vector2.one;
        rtGo.offsetMin = Vector2.zero;
        rtGo.offsetMax = Vector2.zero;

        var ui = go.AddComponent<GameOverUI>();
        ui.EnsureUIBuilt();
        return ui;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene == "menuPrincipal") return;

        EnsureEventSystemExists();
        EnsureInstance();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        isGameOverActive = false;

        EnsureEventSystemExists();
        EnsureUIBuilt();

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        if (btnMainMenu != null)
        {
            btnMainMenu.onClick.RemoveListener(HandleMainMenuClick);
            btnMainMenu.onClick.AddListener(HandleMainMenuClick);
        }

        if (btnPlayAgain != null)
        {
            btnPlayAgain.onClick.RemoveListener(HandlePlayAgainClick);
            btnPlayAgain.onClick.AddListener(HandlePlayAgainClick);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (btnMainMenu != null)
        {
            btnMainMenu.onClick.RemoveListener(HandleMainMenuClick);
        }

        if (btnPlayAgain != null)
        {
            btnPlayAgain.onClick.RemoveListener(HandlePlayAgainClick);
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame) Show(MatchResultReason.SurviveTime, 18);
            else if (kb.f2Key.wasPressedThisFrame) Show(MatchResultReason.DefeatedAllEnemies, 28);
            else if (kb.f3Key.wasPressedThisFrame) Show(MatchResultReason.OutOfLives, 8);
            else if (kb.f4Key.wasPressedThisFrame) Show(MatchResultReason.TowerDestroyed, 12);
            else if (kb.f5Key.wasPressedThisFrame || (isGameOverActive && kb.escapeKey.wasPressedThisFrame)) Hide();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F1)) Show(MatchResultReason.SurviveTime, 18);
        else if (Input.GetKeyDown(KeyCode.F2)) Show(MatchResultReason.DefeatedAllEnemies, 28);
        else if (Input.GetKeyDown(KeyCode.F3)) Show(MatchResultReason.OutOfLives, 8);
        else if (Input.GetKeyDown(KeyCode.F4)) Show(MatchResultReason.TowerDestroyed, 12);
        else if (Input.GetKeyDown(KeyCode.F5) || (isGameOverActive && Input.GetKeyDown(KeyCode.Escape))) Hide();
#endif
    }

    private void LateUpdate()
    {
        if (isGameOverActive)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
        }
    }

    [ContextMenu("Reconstruir UI")]
    public void RebuildUI()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        rootPanel = null;
        EnsureUIBuilt();
    }

    public void EnsureUIBuilt()
    {
        if (rootPanel != null) return;

        EnsureEventSystemExists();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = GameObject.Find("GameOverCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(800, 600);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvas = canvasGo.GetComponent<Canvas>();
            }
            transform.SetParent(canvas.transform, false);
        }

        transform.SetAsLastSibling();

        RectTransform rtThis = GetComponent<RectTransform>();
        if (rtThis == null) rtThis = gameObject.AddComponent<RectTransform>();
        rtThis.anchorMin = Vector2.zero;
        rtThis.anchorMax = Vector2.one;
        rtThis.offsetMin = Vector2.zero;
        rtThis.offsetMax = Vector2.zero;

        // 1. Panel de fondo que cubre la totalidad de la pantalla
        rootPanel = new GameObject("GameOverRootPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        rootPanel.transform.SetParent(transform, false);
        RectTransform rtRoot = rootPanel.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = new Vector2(-1500, -1500);
        rtRoot.offsetMax = new Vector2(1500, 1500);
        Image imgRoot = rootPanel.GetComponent<Image>();
        imgRoot.color = new Color(0.02f, 0.03f, 0.05f, 0.96f);
        imgRoot.raycastTarget = true;
        rootCanvasGroup = rootPanel.GetComponent<CanvasGroup>();

        // 2. Tarjeta / Modal central solida (560 x 350 px)
        GameObject cardGo = new GameObject("GameOverCard", typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(rootPanel.transform, false);
        cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(560, 350);
        cardRect.anchoredPosition = Vector2.zero;
        Image cardImg = cardGo.GetComponent<Image>();
        cardImg.color = new Color(0.08f, 0.10f, 0.14f, 1.0f);
        cardImg.raycastTarget = true;

        // Barra de acento neon superior
        GameObject accentGo = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
        accentGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtAccent = accentGo.GetComponent<RectTransform>();
        rtAccent.anchorMin = new Vector2(0f, 1f);
        rtAccent.anchorMax = new Vector2(1f, 1f);
        rtAccent.sizeDelta = new Vector2(0, 4);
        rtAccent.anchoredPosition = new Vector2(0, -2);
        accentImage = accentGo.GetComponent<Image>();
        accentImage.color = victoryColor;
        accentImage.raycastTarget = false;

        // Badge superior ("MISION CUMPLIDA" / "MISION FALLIDA")
        GameObject badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtBadge = badgeGo.GetComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(0f, 1f);
        rtBadge.anchorMax = new Vector2(1f, 1f);
        rtBadge.sizeDelta = new Vector2(-40, 24);
        rtBadge.anchoredPosition = new Vector2(0, -26);
        badgeText = badgeGo.GetComponent<TextMeshProUGUI>();
        badgeText.fontSize = 12;
        badgeText.fontStyle = FontStyles.Bold;
        badgeText.alignment = TextAlignmentOptions.Center;
        badgeText.characterSpacing = 8;
        badgeText.text = "MISION CUMPLIDA";
        badgeText.color = victoryColor;
        badgeText.raycastTarget = false;

        // 3. Titulo ("VICTORIA" / "DERROTA")
        GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtTitle = titleGo.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 1f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.sizeDelta = new Vector2(-40, 56);
        rtTitle.anchoredPosition = new Vector2(0, -62);
        titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.fontSize = 46;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.characterSpacing = 6;
        titleText.text = victoryTitle;
        titleText.color = victoryColor;
        titleText.raycastTarget = false;

        // 4. Subtitulo con descripcion
        GameObject subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtSub = subGo.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0f, 1f);
        rtSub.anchorMax = new Vector2(1f, 1f);
        rtSub.sizeDelta = new Vector2(-60, 36);
        rtSub.anchoredPosition = new Vector2(0, -114);
        subtitleText = subGo.GetComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 14;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = new Color(0.72f, 0.78f, 0.86f, 1f);
        subtitleText.text = victorySurviveSubtitle;
        subtitleText.raycastTarget = false;

        // 5. Tarjeta de estadistica: "ENEMIGOS ELIMINADOS: 18"
        GameObject singleStatGo = new GameObject("SingleStatPill", typeof(RectTransform), typeof(Image));
        singleStatGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtStatPill = singleStatGo.GetComponent<RectTransform>();
        rtStatPill.anchorMin = new Vector2(0.5f, 1f);
        rtStatPill.anchorMax = new Vector2(0.5f, 1f);
        rtStatPill.sizeDelta = new Vector2(340, 44);
        rtStatPill.anchoredPosition = new Vector2(0, -170);
        Image imgPill = singleStatGo.GetComponent<Image>();
        imgPill.color = new Color(0.04f, 0.05f, 0.08f, 1.0f);
        imgPill.raycastTarget = false;

        GameObject statTextGo = new GameObject("KillsValueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statTextGo.transform.SetParent(singleStatGo.transform, false);
        RectTransform rtKText = statTextGo.GetComponent<RectTransform>();
        rtKText.anchorMin = Vector2.zero;
        rtKText.anchorMax = Vector2.one;
        rtKText.offsetMin = Vector2.zero;
        rtKText.offsetMax = Vector2.zero;
        killsValueText = statTextGo.GetComponent<TextMeshProUGUI>();
        killsValueText.fontSize = 16;
        killsValueText.fontStyle = FontStyles.Bold;
        killsValueText.alignment = TextAlignmentOptions.Center;
        killsValueText.characterSpacing = 2;
        killsValueText.color = new Color(0.35f, 0.95f, 0.6f, 1f);
        killsValueText.text = "ENEMIGOS ELIMINADOS: 0";
        killsValueText.raycastTarget = false;

        // 6. Botones de Accion
        btnPlayAgain = CrearBoton(cardGo.transform, "BtnPlayAgain", "JUGAR DE NUEVO", new Vector2(-125, -265), new Color(0.14f, 0.68f, 0.38f, 1f));
        btnMainMenu = CrearBoton(cardGo.transform, "BtnMainMenu", "MENU PRINCIPAL", new Vector2(125, -265), new Color(0.20f, 0.24f, 0.32f, 1f));

        btnPlayAgain.onClick.AddListener(HandlePlayAgainClick);
        btnMainMenu.onClick.AddListener(HandleMainMenuClick);

        rootPanel.SetActive(false);
    }

    private Button CrearBoton(Transform parent, string name, string label, Vector2 pos, Color baseColor)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(220, 48);
        rt.anchoredPosition = pos;

        Image img = btnGo.GetComponent<Image>();
        img.color = baseColor;
        img.raycastTarget = true;

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.25f;
        colors.pressedColor = baseColor * 0.75f;
        colors.selectedColor = baseColor;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform rtText = textGo.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = Vector2.zero;
        rtText.offsetMax = Vector2.zero;

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 13;
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 2;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
        tmp.raycastTarget = false;

        return btn;
    }

    public void Show(MatchResultReason reason, int kills = 15)
    {
        bool isVictory = (reason == MatchResultReason.SurviveTime || reason == MatchResultReason.DefeatedAllEnemies);

        string reasonSubtitle = reason switch
        {
            MatchResultReason.SurviveTime => victorySurviveSubtitle,
            MatchResultReason.DefeatedAllEnemies => victoryAllEnemiesSubtitle,
            MatchResultReason.OutOfLives => defeatOutOfLivesSubtitle,
            MatchResultReason.TowerDestroyed => defeatTowerDestroyedSubtitle,
            _ => string.Empty
        };

        DisplayUI(isVictory, reasonSubtitle, kills);
    }

    public void Show(MatchResultReason reason, int kills, int deaths)
    {
        Show(reason, kills);
    }

    public void Show(MatchResultReason reason, int kills, int deaths, string time)
    {
        Show(reason, kills);
    }

    public void Show(bool isVictory)
    {
        string defaultSubtitle = isVictory ? victorySurviveSubtitle : defeatOutOfLivesSubtitle;
        DisplayUI(isVictory, defaultSubtitle, -1);
    }

    public void Show(bool isVictory, int kills, int deaths)
    {
        string defaultSubtitle = isVictory ? victorySurviveSubtitle : defeatOutOfLivesSubtitle;
        DisplayUI(isVictory, defaultSubtitle, kills);
    }

    private void DisplayUI(bool isVictory, string subtitle, int kills)
    {
        EnsureEventSystemExists();

        if (transform.parent != null && !transform.parent.gameObject.activeSelf)
        {
            transform.parent.gameObject.SetActive(true);
        }
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        EnsureUIBuilt();
        isGameOverActive = true;

        transform.SetAsLastSibling();
        if (rootPanel != null)
        {
            rootPanel.transform.SetAsLastSibling();
            rootPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Ocultar panel de derrota individual si estaba activo
        if (PersonalLosePanel.Instance != null)
        {
            PersonalLosePanel.Instance.gameObject.SetActive(false);
        }

        // Cancelar cualquier corrutina de desconexion por muerte individual
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include);
        foreach (var ph in allPlayers)
        {
            if (ph != null) ph.CancelReturnToMenu();
        }

        OcultarHudsDelJuego();
        DesactivarControlesJugador();
        DetenerActividadMundo();

        // Asegurar que los botones esten habilitados y con el texto correcto
        if (btnPlayAgain != null)
        {
            btnPlayAgain.interactable = true;
            var tmp = btnPlayAgain.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "JUGAR DE NUEVO";
        }
        if (btnMainMenu != null)
        {
            btnMainMenu.interactable = true;
            var tmp = btnMainMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "MENU PRINCIPAL";
        }

        Color mainColor = isVictory ? victoryColor : defeatColor;

        if (badgeText != null)
        {
            badgeText.text = isVictory ? "MISION CUMPLIDA" : "MISION FALLIDA";
            badgeText.color = mainColor;
        }

        if (titleText != null)
        {
            titleText.text = isVictory ? victoryTitle : defeatTitle;
            titleText.color = mainColor;
        }

        if (accentImage != null)
        {
            accentImage.color = mainColor;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
        }

        if (killsValueText != null)
        {
            if (killsValueText.transform.parent != null && killsValueText.transform.parent.name == "SingleStatPill")
            {
                killsValueText.transform.parent.gameObject.SetActive(kills >= 0);
            }
            killsValueText.color = isVictory ? new Color(0.35f, 0.95f, 0.6f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
        }

        // Animacion de entrada suave y conteo de bajas
        if (gameObject.activeInHierarchy)
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateEntry());

            if (kills >= 0)
            {
                if (tickerCoroutine != null) StopCoroutine(tickerCoroutine);
                tickerCoroutine = StartCoroutine(AnimateStatsTicker(kills));
            }
        }
        else
        {
            if (rootCanvasGroup != null) rootCanvasGroup.alpha = 1f;
            if (kills >= 0 && killsValueText != null) killsValueText.text = $"ENEMIGOS ELIMINADOS: [ {kills} ]";
        }

        OnGameOverShown?.Invoke();
    }

    private void DetenerActividadMundo()
    {
        EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsInactive.Exclude);
        foreach (var sp in spawners)
        {
            if (sp != null)
            {
                sp.StopAllCoroutines();
                sp.enabled = false;
            }
        }

        EnemyCombat[] combats = FindObjectsByType<EnemyCombat>(FindObjectsInactive.Exclude);
        foreach (var c in combats)
        {
            if (c != null)
            {
                c.enabled = false;
                if (c.audioSource != null && c.audioSource.isPlaying)
                {
                    c.audioSource.Stop();
                }
                Animator anim = c.GetComponentInChildren<Animator>();
                if (anim != null)
                {
                    anim.speed = 0f;
                }
            }
        }

        Ai[] ais = FindObjectsByType<Ai>(FindObjectsInactive.Exclude);
        foreach (var ai in ais)
        {
            if (ai != null)
            {
                ai.enabled = false;
            }
        }

        UnityEngine.AI.NavMeshAgent[] agents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsInactive.Exclude);
        foreach (var agent in agents)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        Shoot[] shoots = FindObjectsByType<Shoot>(FindObjectsInactive.Exclude);
        foreach (var sh in shoots)
        {
            if (sh != null) sh.enabled = false;
        }
    }

    private IEnumerator AnimateEntry()
    {
        float duration = 0.28f;
        float elapsed = 0f;

        if (rootCanvasGroup != null) rootCanvasGroup.alpha = 0f;
        if (cardRect != null) cardRect.localScale = new Vector3(0.9f, 0.9f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (rootCanvasGroup != null) rootCanvasGroup.alpha = ease;
            if (cardRect != null) cardRect.localScale = Vector3.Lerp(new Vector3(0.9f, 0.9f, 1f), Vector3.one, ease);

            yield return null;
        }

        if (rootCanvasGroup != null) rootCanvasGroup.alpha = 1f;
        if (cardRect != null) cardRect.localScale = Vector3.one;
    }

    private IEnumerator AnimateStatsTicker(int targetKills)
    {
        float duration = 0.7f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int cur = Mathf.RoundToInt(Mathf.Lerp(0, targetKills, t));

            if (killsValueText != null)
            {
                killsValueText.text = $"ENEMIGOS ELIMINADOS: [ {cur} ]";
            }

            yield return null;
        }

        if (killsValueText != null)
        {
            killsValueText.text = $"ENEMIGOS ELIMINADOS: [ {targetKills} ]";
        }
    }

    private void OcultarHudsDelJuego()
    {
        SafeHide(FindFirstObjectByType<PlayerHealthUI>());
        SafeHide(FindFirstObjectByType<AmmoUI>());
        SafeHide(FindFirstObjectByType<TowerUi>());
        SafeHide(FindFirstObjectByType<sesionCodeUI>());

        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude);
        foreach (var t in texts)
        {
            if (t != null && t.gameObject != null && t.gameObject.name.ToLower().Contains("timer") && !t.transform.IsChildOf(transform))
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void SafeHide(MonoBehaviour mb)
    {
        if (mb == null) return;
        mb.enabled = false;
        if (mb.GetComponent<Canvas>() == null && mb.gameObject != gameObject && !transform.IsChildOf(mb.transform))
        {
            mb.gameObject.SetActive(false);
        }
    }

    private void DesactivarControlesJugador()
    {
        DesactivarCamarasJugador();

        PlayerMovementCC[] movements = FindObjectsByType<PlayerMovementCC>(FindObjectsInactive.Exclude);
        foreach (var pm in movements)
        {
            if (pm != null) pm.enabled = false;
        }

        Shoot[] shoots = FindObjectsByType<Shoot>(FindObjectsInactive.Exclude);
        foreach (var sh in shoots)
        {
            if (sh != null) sh.enabled = false;
        }

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.PlayerInput[] inputs = FindObjectsByType<UnityEngine.InputSystem.PlayerInput>(FindObjectsInactive.Exclude);
        foreach (var input in inputs)
        {
            if (input != null) input.DeactivateInput();
        }
#endif
    }

    private void DesactivarCamarasJugador()
    {
        MonoBehaviour[] scripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude);
        foreach (MonoBehaviour mb in scripts)
        {
            if (mb != null && mb.GetType().Name == "CameraControllerFPS")
            {
                mb.enabled = false;
            }
        }
    }

    public void RestaurarControlesJugador()
    {
        PlayerMovementCC[] movements = FindObjectsByType<PlayerMovementCC>(FindObjectsInactive.Include);
        foreach (var pm in movements)
        {
            if (pm != null) pm.enabled = true;
        }

        Shoot[] shoots = FindObjectsByType<Shoot>(FindObjectsInactive.Include);
        foreach (var sh in shoots)
        {
            if (sh != null) sh.enabled = true;
        }

        CameraControllerFPS[] cams = FindObjectsByType<CameraControllerFPS>(FindObjectsInactive.Include);
        foreach (var cam in cams)
        {
            if (cam != null)
            {
                cam.enabled = true;
                if (cam.IsOwner)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.PlayerInput[] inputs = FindObjectsByType<UnityEngine.InputSystem.PlayerInput>(FindObjectsInactive.Include);
        foreach (var input in inputs)
        {
            if (input != null) input.ActivateInput();
        }
#endif
    }

    public void Hide()
    {
        isGameOverActive = false;

        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (tickerCoroutine != null) StopCoroutine(tickerCoroutine);

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        RestaurarControlesJugador();
    }

    private void HandleMainMenuClick()
    {
        Debug.Log("[GameOverUI] HandleMainMenuClick invocado.");

        if (btnMainMenu != null)
        {
            btnMainMenu.interactable = false;
            var tmp = btnMainMenu.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "SALIENDO...";
        }

        if (OnReturnToMenuRequested != null)
        {
            OnReturnToMenuRequested.Invoke();
            return;
        }

        CerrarRedYVolverAlMenu();
    }

    private void HandlePlayAgainClick()
    {
        Debug.Log("[GameOverUI] HandlePlayAgainClick invocado.");

        if (OnPlayAgainRequested != null)
        {
            OnPlayAgainRequested.Invoke();
            return;
        }

        ReiniciarPartida();
    }

    private void CerrarRedYVolverAlMenu()
    {
        Debug.Log("[GameOverUI] Cerrando conexion de red y volviendo a: " + mainMenuSceneName);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LimpiarLobbyLocal();
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void ReiniciarPartida()
    {
        Debug.Log("[GameOverUI] ReiniciarPartida ejecutando...");

        if (btnPlayAgain != null)
        {
            btnPlayAgain.interactable = false;
            var tmp = btnPlayAgain.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = "REINICIANDO...";
        }

        // Si estamos en una partida de red activa
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartMatch();
                    return;
                }

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    Debug.Log("[GameOverUI] Host reiniciando escena con NetworkSceneManager: " + SceneManager.GetActiveScene().name);
                    NetworkManager.Singleton.SceneManager.LoadScene(
                        SceneManager.GetActiveScene().name,
                        LoadSceneMode.Single
                    );
                    return;
                }
            }
            else if (NetworkManager.Singleton.IsConnectedClient)
            {
                Debug.Log("[GameOverUI] Cliente solicitando reinicio de partida al Host...");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RequestRestartMatchServerRpc();
                    return;
                }
            }
        }

        // Modo offline / local
        Debug.Log("[GameOverUI] Recargando escena localmente: " + SceneManager.GetActiveScene().name);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

#if UNITY_EDITOR
    [ContextMenu("Probar Victoria: Tiempo Sobrevivido")]
    public void TestVictorySurvive()
    {
        Show(MatchResultReason.SurviveTime, 18);
    }

    [ContextMenu("Probar Victoria: Enemigos Eliminados")]
    public void TestVictoryAllEnemies()
    {
        Show(MatchResultReason.DefeatedAllEnemies, 30);
    }

    [ContextMenu("Probar Derrota: Sin Vidas")]
    public void TestDefeatNoLives()
    {
        Show(MatchResultReason.OutOfLives, 8);
    }

    [ContextMenu("Probar Derrota: Torre Destruida")]
    public void TestDefeatTower()
    {
        Show(MatchResultReason.TowerDestroyed, 12);
    }

    [ContextMenu("Ocultar Pantalla")]
    public void TestHide()
    {
        Hide();
    }
#endif
}
