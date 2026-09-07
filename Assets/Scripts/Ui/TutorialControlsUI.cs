using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialControlsUI : MonoBehaviour
{
    public static TutorialControlsUI Instance { get; private set; }

    [Header("Referencias Principales")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Button btnCerrar;

    private CanvasGroup canvasGroup;
    private RectTransform cardRect;
    private bool isVisible = false;

    public static TutorialControlsUI EnsureInstance(Canvas parentCanvas = null)
    {
        if (Instance != null) return Instance;

        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas == null)
            {
                GameObject canvasGo = new GameObject("HUD", typeof(Canvas), typeof(GraphicRaycaster));
                parentCanvas = canvasGo.GetComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        GameObject go = new GameObject("TutorialControlsUI", typeof(RectTransform));
        go.transform.SetParent(parentCanvas.transform, false);
        go.transform.SetAsLastSibling();

        RectTransform rtGo = go.GetComponent<RectTransform>();
        rtGo.anchorMin = Vector2.zero;
        rtGo.anchorMax = Vector2.one;
        rtGo.offsetMin = Vector2.zero;
        rtGo.offsetMax = Vector2.zero;

        var ui = go.AddComponent<TutorialControlsUI>();
        ui.EnsureUIBuilt();
        return ui;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureUIBuilt();

        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isVisible) return;

#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Hide();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
#endif
    }

    public void Show()
    {
        if (cardRect == null || cardRect.sizeDelta.y != 340f)
        {
            if (rootPanel != null) Destroy(rootPanel);
            rootPanel = null;
        }
        EnsureUIBuilt();
        isVisible = true;

        transform.SetAsLastSibling();
        if (rootPanel != null)
        {
            rootPanel.transform.SetAsLastSibling();
            rootPanel.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        isVisible = false;
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }

    public void EnsureUIBuilt()
    {
        if (rootPanel != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
            if (parentCanvas != null)
            {
                transform.SetParent(parentCanvas.transform, false);
            }
        }

        transform.SetAsLastSibling();

        RectTransform rtThis = GetComponent<RectTransform>();
        if (rtThis == null) rtThis = gameObject.AddComponent<RectTransform>();
        rtThis.anchorMin = Vector2.zero;
        rtThis.anchorMax = Vector2.one;
        rtThis.offsetMin = Vector2.zero;
        rtThis.offsetMax = Vector2.zero;

        // Limpiar cualquier residuo previo
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        // 1. Panel de fondo oscuro
        rootPanel = new GameObject("TutorialRootPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        rootPanel.transform.SetParent(transform, false);
        RectTransform rtRoot = rootPanel.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = new Vector2(-1500, -1500);
        rtRoot.offsetMax = new Vector2(1500, 1500);

        Image imgRoot = rootPanel.GetComponent<Image>();
        imgRoot.color = new Color(0.03f, 0.04f, 0.06f, 0.96f);
        canvasGroup = rootPanel.GetComponent<CanvasGroup>();

        // 2. Tarjeta central compacta (600 x 340 px) para que entre en cualquier resolución
        GameObject cardGo = new GameObject("TutorialCard", typeof(RectTransform), typeof(Image));
        cardGo.transform.SetParent(rootPanel.transform, false);
        cardRect = cardGo.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(600, 340);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardImg = cardGo.GetComponent<Image>();
        cardImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

        // Barra de acento superior neón cian
        GameObject accentGo = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
        accentGo.transform.SetParent(cardGo.transform, false);
        RectTransform rtAccent = accentGo.GetComponent<RectTransform>();
        rtAccent.anchorMin = new Vector2(0f, 1f);
        rtAccent.anchorMax = new Vector2(1f, 1f);
        rtAccent.pivot = new Vector2(0.5f, 1f);
        rtAccent.sizeDelta = new Vector2(0, 3);
        rtAccent.anchoredPosition = Vector2.zero;
        Image accentImg = accentGo.GetComponent<Image>();
        accentImg.color = new Color(0.2f, 0.65f, 1.0f, 1.0f);

        // Botón cerrar (X) en esquina superior derecha - Letra X estándar para evitar problemas de fuentes
        Button btnX = CrearBoton(cardGo.transform, "BtnCerrarX", "X", new Vector2(-10, -10), new Vector2(26, 26), new Color(0.25f, 0.30f, 0.40f, 0.9f), new Vector2(1f, 1f), new Vector2(1f, 1f), 13);
        btnX.onClick.AddListener(Hide);

        // Título Principal
        CrearTexto(cardGo.transform, "TitleText", "TUTORIAL Y CONTROLES", 17, FontStyles.Bold, Color.white, new Vector2(0, -12), new Vector2(520, 24), TextAlignmentOptions.Center, false);

        // Subtítulo
        CrearTexto(cardGo.transform, "SubtitleText", "Aprende las mecanicas de defensa y los comandos del soldado", 10.5f, FontStyles.Normal, new Color(0.7f, 0.75f, 0.85f, 1f), new Vector2(0, -35), new Vector2(520, 16), TextAlignmentOptions.Center, false);

        // 3. Contenedor de dos columnas (altura 215px)
        // Columna Izquierda: Cómo Jugar (Objetivos)
        GameObject colLeft = new GameObject("ColObjetivos", typeof(RectTransform), typeof(Image));
        colLeft.transform.SetParent(cardGo.transform, false);
        RectTransform rtLeft = colLeft.GetComponent<RectTransform>();
        rtLeft.anchorMin = new Vector2(0.5f, 1f);
        rtLeft.anchorMax = new Vector2(0.5f, 1f);
        rtLeft.pivot = new Vector2(0.5f, 1f);
        rtLeft.sizeDelta = new Vector2(275, 215);
        rtLeft.anchoredPosition = new Vector2(-142, -56);
        Image imgLeft = colLeft.GetComponent<Image>();
        imgLeft.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);

        CrearTexto(colLeft.transform, "HeaderObj", "OBJETIVOS DE LA MISION", 11.5f, FontStyles.Bold, new Color(0.25f, 0.9f, 0.55f, 1f), new Vector2(0, -8), new Vector2(255, 18), TextAlignmentOptions.Left, false);

        string textoObjetivos =
            "<b><color=#55FF99>1. Protege la Torre</color></b>\n" +
            "Es el nucleo de defensa. Si su vida cae a cero, la mision termina en derrota.\n\n" +
            "<b><color=#55FF99>2. Resiste el Tiempo</color></b>\n" +
            "Defiende la base hasta que el temporizador llegue a 0 para ganar.\n\n" +
            "<b><color=#55FF99>3. Juego Cooperativo</color></b>\n" +
            "Juega con companeros. Si todos los soldados caen, se pierde la partida.\n\n" +
            "<b><color=#55FF99>4. Destruye las Aranas</color></b>\n" +
            "Elimina las oleadas enemigas antes de que alcancen las defensas.";

        CrearTexto(colLeft.transform, "BodyObj", textoObjetivos, 10f, FontStyles.Normal, new Color(0.82f, 0.86f, 0.92f, 1f), new Vector2(0, -28), new Vector2(255, 180), TextAlignmentOptions.TopLeft, true);

        // Columna Derecha: Controles
        GameObject colRight = new GameObject("ColControles", typeof(RectTransform), typeof(Image));
        colRight.transform.SetParent(cardGo.transform, false);
        RectTransform rtRight = colRight.GetComponent<RectTransform>();
        rtRight.anchorMin = new Vector2(0.5f, 1f);
        rtRight.anchorMax = new Vector2(0.5f, 1f);
        rtRight.pivot = new Vector2(0.5f, 1f);
        rtRight.sizeDelta = new Vector2(275, 215);
        rtRight.anchoredPosition = new Vector2(142, -56);
        Image imgRight = colRight.GetComponent<Image>();
        imgRight.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);

        CrearTexto(colRight.transform, "HeaderCtrl", "CONTROLES DE COMBATE", 11.5f, FontStyles.Bold, new Color(0.35f, 0.75f, 1f, 1f), new Vector2(0, -8), new Vector2(255, 18), TextAlignmentOptions.Left, false);

        string textoControles =
            "<b><color=#55DDFF>[ W, A, S, D ]</color></b>    Mover soldado\n\n" +
            "<b><color=#55DDFF>[ ESPACIO ]</color></b>     Saltar obstaculos\n\n" +
            "<b><color=#55DDFF>[ MOUSE ]</color></b>       Apuntar y orientar camara\n\n" +
            "<b><color=#55DDFF>[ CLIC IZQ ]</color></b>    Disparar arma\n\n" +
            "<b><color=#55DDFF>[ TECLA R ]</color></b>     Recargar municion\n\n" +
            "<b><color=#55DDFF>[ 1, 2, 3 ]</color></b>      Cambiar de arma\n\n" +
            "<b><color=#55DDFF>[ ESCAPE ]</color></b>      Cerrar ventana";

        CrearTexto(colRight.transform, "BodyCtrl", textoControles, 10.5f, FontStyles.Normal, new Color(0.88f, 0.92f, 0.96f, 1f), new Vector2(0, -28), new Vector2(255, 180), TextAlignmentOptions.TopLeft, true);

        // 4. Botón inferior de cerrar - perfectamente encuadrado adentro de la tarjeta
        btnCerrar = CrearBoton(cardGo.transform, "BtnCerrar", "ENTENDIDO / VOLVER AL MENU", new Vector2(0, 14), new Vector2(240, 30), new Color(0.14f, 0.65f, 0.38f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 11);
        btnCerrar.onClick.AddListener(Hide);

        rootPanel.SetActive(false);
    }

    private static TMP_FontAsset cachedFont;
    private static TMP_FontAsset GetFont()
    {
        if (cachedFont != null) return cachedFont;
        var allTmps = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        foreach (var t in allTmps)
        {
            if (t != null && t.font != null)
            {
                cachedFont = t.font;
                return cachedFont;
            }
        }
        if (TMP_Settings.defaultFontAsset != null)
        {
            cachedFont = TMP_Settings.defaultFontAsset;
            return cachedFont;
        }
        return null;
    }

    private TextMeshProUGUI CrearTexto(Transform parent, string name, string content, float fontSize, FontStyles style, Color color, Vector2 pos, Vector2 size, TextAlignmentOptions align, bool wordWrap = true)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        var font = GetFont();
        if (font != null) tmp.font = font;

        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.enableWordWrapping = wordWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.richText = true;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CrearBoton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color baseColor, Vector2 anchor, Vector2 pivot, float fontSize = 12)
    {
        GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        Image img = btnGo.GetComponent<Image>();
        img.color = baseColor;

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
        var font = GetFont();
        if (font != null) tmp.font = font;

        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }
}
