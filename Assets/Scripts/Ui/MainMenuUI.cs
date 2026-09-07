using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button btnCrearPartida;
    public Button btnUnirse;
    public Button btnSalir;
    public TMP_InputField inputCodigoPartida;
    public TMP_Text statusText;
    public AudioSource audioSource;
    public AudioClip clickSound;

    [Header("Tutorial y Controles")]
    public Button btnTutoriales;

    [Header("Referencia a Lobby")]
    public LobbyManager lobbyManager;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        btnCrearPartida.onClick.AddListener(OnCrearPartida);
        btnUnirse.onClick.AddListener(OnUnirse);
        btnSalir.onClick.AddListener(OnSalir);

        AsegurarBotonTutoriales();
        if (btnTutoriales != null)
        {
            btnTutoriales.onClick.AddListener(OnAbrirTutoriales);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    private void AsegurarBotonTutoriales()
    {
        if (btnTutoriales != null) return;

        // Determinar el padre adecuado (el mismo contenedor que btnSalir o el Canvas principal)
        Transform parentTransform = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (btnSalir != null && btnSalir.transform.parent != null)
        {
            parentTransform = btnSalir.transform.parent;
            if (canvas == null) canvas = btnSalir.GetComponentInParent<Canvas>();
        }
        if (parentTransform == null && canvas != null)
        {
            parentTransform = canvas.transform;
        }
        if (parentTransform == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null) parentTransform = canvas.transform;
        }
        if (parentTransform == null) return;

        // Si btnSalir existe, posicionar el botón de Tutorial en la posición de Salir y desplazar Salir hacia abajo
        Vector2 posTutorial = new Vector2(-242f, -90f);
        Vector2 sizeTutorial = new Vector2(212.4f, 30f);
        if (btnSalir != null)
        {
            RectTransform rtSalir = btnSalir.GetComponent<RectTransform>();
            if (rtSalir != null)
            {
                posTutorial = rtSalir.anchoredPosition;
                sizeTutorial = rtSalir.sizeDelta;
                // Desplazar el botón salir hacia abajo 45 píxeles para hacer lugar al nuevo botón
                rtSalir.anchoredPosition = new Vector2(rtSalir.anchoredPosition.x, rtSalir.anchoredPosition.y - 45f);
            }
        }

        GameObject btnGo = new GameObject("btnTutoriales", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parentTransform, false);
        if (btnSalir != null)
        {
            btnGo.transform.SetSiblingIndex(btnSalir.transform.GetSiblingIndex());
        }

        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeTutorial;
        rt.anchoredPosition = posTutorial;

        Image img = btnGo.GetComponent<Image>();
        Image salirImg = btnSalir != null ? btnSalir.GetComponent<Image>() : null;
        if (salirImg != null)
        {
            img.sprite = salirImg.sprite;
            img.type = salirImg.type;
            img.color = salirImg.color;
            img.material = salirImg.material;
        }
        else
        {
            img.color = Color.white;
        }

        Button btn = btnGo.GetComponent<Button>();
        if (btnSalir != null)
        {
            btn.transition = btnSalir.transition;
            btn.colors = btnSalir.colors;
            btn.spriteState = btnSalir.spriteState;
        }
        else
        {
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f);
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
            colors.selectedColor = new Color(0.96f, 0.96f, 0.96f);
            btn.colors = colors;
        }

        GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform rtText = textGo.GetComponent<RectTransform>();
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = Vector2.zero;
        rtText.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI salirTmp = btnSalir != null ? btnSalir.GetComponentInChildren<TextMeshProUGUI>() : null;
        if (salirTmp != null)
        {
            tmp.font = salirTmp.font;
            tmp.fontSharedMaterial = salirTmp.fontSharedMaterial;
            tmp.fontSize = Mathf.Min(salirTmp.fontSize, 13f);
            tmp.color = salirTmp.color;
        }
        else
        {
            tmp.fontSize = 13f;
            tmp.color = new Color(0.12f, 0.15f, 0.2f, 1f);
        }
        tmp.text = "TUTORIAL Y CONTROLES";
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        btnTutoriales = btn;
    }

    private void OnAbrirTutoriales()
    {
        PlayClickSound();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && btnSalir != null) canvas = btnSalir.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        TutorialControlsUI ui = TutorialControlsUI.EnsureInstance(canvas);
        if (ui != null)
        {
            ui.Show();
        }
    }

    private void OnCrearPartida()
    {
        SetButtonsInteractable(false);

        if (LobbyManager.Instance == null)
        {
            Debug.LogError("No existe LobbyManager.");
            SetButtonsInteractable(true);
            return;
        }

        PlayClickSound();

        LobbyManager.Instance.CrearLobby();
    }

    private void OnUnirse()
    {
        string codigo = inputCodigoPartida.text.Trim().ToUpper();

        SetButtonsInteractable(false);

        if (LobbyManager.Instance == null)
        {
            Debug.LogError("No existe LobbyManager.");
            SetButtonsInteractable(true);
            return;
        }

        PlayClickSound();

        LobbyManager.Instance.UnirseLobby(codigo);
    }

    private void OnSalir()
    {
        PlayClickSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetButtonsInteractable(bool interactable)
    {
        btnCrearPartida.interactable = interactable;
        btnUnirse.interactable = interactable;
        if (btnTutoriales != null) btnTutoriales.interactable = interactable;
    }
}