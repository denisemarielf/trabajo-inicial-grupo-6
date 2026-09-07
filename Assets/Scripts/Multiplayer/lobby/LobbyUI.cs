using System.Collections;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Referencias de UI Serializadas")]
    [SerializeField] private TMP_Text textoCodigo;
    [SerializeField] private TMP_Text textoJugadores;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private GameObject botonIniciarPartida;
    [SerializeField] private RelayConnectionManager relayManager;

    [Header("Audio")]
    public AudioClip clickSound;
    public AudioSource audioSource;

    private float tiempoActualizacion = 0f;
    private float intervaloActualizacion = 0.5f;
    private bool clienteConectandose = false;

    // Componentes del diseño moderno
    private RectTransform cardRect;
    private TMP_Text btnCopiarText;
    private GameObject bannerEsperandoHost;
    private Button botonSalirRef;

    private void Start()
    {
        Debug.Log("LOBBY UI INICIADO");

        AsegurarDisenoModerno();

        if (LobbyManager.Instance != null)
        {
            Debug.Log("LobbyCode desde Start: " + LobbyManager.Instance.GetLobbyCode());
            Debug.Log("Cantidad desde Start: " + LobbyManager.Instance.GetPlayerCount());
            Debug.Log("Es Host desde Start: " + LobbyManager.Instance.IsHost());
        }

        ActualizarUI();
    }

    private void Update()
    {
        tiempoActualizacion += Time.deltaTime;

        if (tiempoActualizacion >= intervaloActualizacion)
        {
            tiempoActualizacion = 0f;
            ActualizarUI();
            ComprobarCliente();
        }
    }

    private void AsegurarDisenoModerno()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // 1. Ocultar el texto estático duplicado "TextoHost" si existe
        Transform textoHostOld = canvas.transform.Find("TextoHost");
        if (textoHostOld != null)
        {
            textoHostOld.gameObject.SetActive(false);
        }

        // 2. Fondo oscuro inmersivo (tapa el cielo por defecto)
        Transform existingBg = canvas.transform.Find("LobbyBackground");
        GameObject bgGo;
        if (existingBg != null)
        {
            bgGo = existingBg.gameObject;
        }
        else
        {
            bgGo = new GameObject("LobbyBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform, false);
            bgGo.transform.SetAsFirstSibling();
            RectTransform rtBg = bgGo.GetComponent<RectTransform>();
            rtBg.anchorMin = Vector2.zero;
            rtBg.anchorMax = Vector2.one;
            rtBg.offsetMin = Vector2.zero;
            rtBg.offsetMax = Vector2.zero;
            Image imgBg = bgGo.GetComponent<Image>();
            imgBg.color = new Color(0.04f, 0.05f, 0.08f, 0.98f);
        }

        // 3. Tarjeta central elegante (520 x 380 px)
        Transform existingCard = canvas.transform.Find("LobbyCard");
        GameObject cardGo;
        if (existingCard != null)
        {
            cardGo = existingCard.gameObject;
        }
        else
        {
            cardGo = new GameObject("LobbyCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(canvas.transform, false);
            cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(520, 380);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.10f, 0.14f, 0.98f);

            // Barra superior neón cian
            GameObject accentGo = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(cardGo.transform, false);
            RectTransform rtAccent = accentGo.GetComponent<RectTransform>();
            rtAccent.anchorMin = new Vector2(0f, 1f);
            rtAccent.anchorMax = new Vector2(1f, 1f);
            rtAccent.pivot = new Vector2(0.5f, 1f);
            rtAccent.sizeDelta = new Vector2(0, 3);
            rtAccent.anchoredPosition = Vector2.zero;
            Image accentImg = accentGo.GetComponent<Image>();
            accentImg.color = new Color(0.2f, 0.7f, 1.0f, 1.0f);
        }

        cardRect = cardGo.GetComponent<RectTransform>();

        // 4. Reparentar y estilizar Título
        Transform tituloOld = canvas.transform.Find("Titulo");
        if (tituloOld != null)
        {
            tituloOld.SetParent(cardGo.transform, false);
            RectTransform rtTitulo = tituloOld.GetComponent<RectTransform>();
            rtTitulo.anchorMin = new Vector2(0.5f, 1f);
            rtTitulo.anchorMax = new Vector2(0.5f, 1f);
            rtTitulo.pivot = new Vector2(0.5f, 1f);
            rtTitulo.anchoredPosition = new Vector2(0, -18);
            rtTitulo.sizeDelta = new Vector2(460, 30);

            TMP_Text tmpTitulo = tituloOld.GetComponent<TMP_Text>();
            if (tmpTitulo != null)
            {
                tmpTitulo.text = "SALA DE ESPERA";
                tmpTitulo.fontSize = 20;
                tmpTitulo.fontStyle = FontStyles.Bold;
                tmpTitulo.color = Color.white;
                tmpTitulo.alignment = TextAlignmentOptions.Center;
            }
        }

        // 5. Reparentar y estilizar TextoEstado (Pill de estado)
        if (textoEstado != null)
        {
            textoEstado.transform.SetParent(cardGo.transform, false);
            RectTransform rtEstado = textoEstado.GetComponent<RectTransform>();
            rtEstado.anchorMin = new Vector2(0.5f, 1f);
            rtEstado.anchorMax = new Vector2(0.5f, 1f);
            rtEstado.pivot = new Vector2(0.5f, 1f);
            rtEstado.anchoredPosition = new Vector2(0, -48);
            rtEstado.sizeDelta = new Vector2(420, 24);
            textoEstado.fontSize = 11.5f;
            textoEstado.alignment = TextAlignmentOptions.Center;
            textoEstado.color = Color.white;
        }

        // 6. Contenedor de Código de Partida (Box destacado)
        Transform existingCodeBox = cardGo.transform.Find("CodeContainer");
        GameObject codeBoxGo;
        if (existingCodeBox != null)
        {
            codeBoxGo = existingCodeBox.gameObject;
        }
        else
        {
            codeBoxGo = new GameObject("CodeContainer", typeof(RectTransform), typeof(Image));
            codeBoxGo.transform.SetParent(cardGo.transform, false);
            RectTransform rtCodeBox = codeBoxGo.GetComponent<RectTransform>();
            rtCodeBox.anchorMin = new Vector2(0.5f, 1f);
            rtCodeBox.anchorMax = new Vector2(0.5f, 1f);
            rtCodeBox.pivot = new Vector2(0.5f, 1f);
            rtCodeBox.anchoredPosition = new Vector2(0, -82);
            rtCodeBox.sizeDelta = new Vector2(440, 100);

            Image imgBox = codeBoxGo.GetComponent<Image>();
            imgBox.color = new Color(0.04f, 0.06f, 0.09f, 0.95f);
            imgBox.raycastTarget = false;

            // Etiqueta superior del box
            GameObject lblGo = new GameObject("LabelCode", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(codeBoxGo.transform, false);
            RectTransform rtLbl = lblGo.GetComponent<RectTransform>();
            rtLbl.anchorMin = new Vector2(0.5f, 1f);
            rtLbl.anchorMax = new Vector2(0.5f, 1f);
            rtLbl.pivot = new Vector2(0.5f, 1f);
            rtLbl.anchoredPosition = new Vector2(0, -10);
            rtLbl.sizeDelta = new Vector2(400, 18);
            TextMeshProUGUI tmpLbl = lblGo.GetComponent<TextMeshProUGUI>();
            if (textoCodigo != null) tmpLbl.font = textoCodigo.font;
            tmpLbl.text = "CÓDIGO DE ACCESO A LA SALA";
            tmpLbl.fontSize = 10f;
            tmpLbl.fontStyle = FontStyles.Bold;
            tmpLbl.color = new Color(0.35f, 0.75f, 1f, 1f);
            tmpLbl.alignment = TextAlignmentOptions.Center;
            tmpLbl.raycastTarget = false;

            // Subtexto aclaratorio
            GameObject helpGo = new GameObject("HelpText", typeof(RectTransform), typeof(TextMeshProUGUI));
            helpGo.transform.SetParent(codeBoxGo.transform, false);
            RectTransform rtHelp = helpGo.GetComponent<RectTransform>();
            rtHelp.anchorMin = new Vector2(0.5f, 0f);
            rtHelp.anchorMax = new Vector2(0.5f, 0f);
            rtHelp.pivot = new Vector2(0.5f, 0f);
            rtHelp.anchoredPosition = new Vector2(0, 8);
            rtHelp.sizeDelta = new Vector2(400, 16);
            TextMeshProUGUI tmpHelp = helpGo.GetComponent<TextMeshProUGUI>();
            if (textoCodigo != null) tmpHelp.font = textoCodigo.font;
            tmpHelp.text = "Comparte este código con tus compañeros para que se unan";
            tmpHelp.fontSize = 9.5f;
            tmpHelp.color = new Color(0.55f, 0.62f, 0.72f, 1f);
            tmpHelp.alignment = TextAlignmentOptions.Center;
            tmpHelp.raycastTarget = false;
        }

        // 7. Reparentar textoCodigo adentro del Box de Código (lado izquierdo)
        if (textoCodigo != null)
        {
            textoCodigo.transform.SetParent(codeBoxGo.transform, false);
            RectTransform rtCode = textoCodigo.GetComponent<RectTransform>();
            rtCode.anchorMin = new Vector2(0f, 0.5f);
            rtCode.anchorMax = new Vector2(0f, 0.5f);
            rtCode.pivot = new Vector2(0f, 0.5f);
            rtCode.anchoredPosition = new Vector2(25, -2);
            rtCode.sizeDelta = new Vector2(280, 40);
            textoCodigo.fontSize = 22;
            textoCodigo.fontStyle = FontStyles.Bold;
            textoCodigo.color = Color.white;
            textoCodigo.alignment = TextAlignmentOptions.Left;
            textoCodigo.raycastTarget = false; // ¡CRUCIAL! No debe bloquear los clicks del botón
        }

        // 8. Botón Copiar al lado del código (lado derecho, al frente de todo)
        Transform existingBtnCopiar = codeBoxGo.transform.Find("BtnCopiar");
        GameObject btnCopiarGo;
        if (existingBtnCopiar != null)
        {
            btnCopiarGo = existingBtnCopiar.gameObject;
        }
        else
        {
            btnCopiarGo = new GameObject("BtnCopiar", typeof(RectTransform), typeof(Image), typeof(Button));
            btnCopiarGo.transform.SetParent(codeBoxGo.transform, false);
            RectTransform rtBtnCopiar = btnCopiarGo.GetComponent<RectTransform>();
            rtBtnCopiar.anchorMin = new Vector2(1f, 0.5f);
            rtBtnCopiar.anchorMax = new Vector2(1f, 0.5f);
            rtBtnCopiar.pivot = new Vector2(1f, 0.5f);
            rtBtnCopiar.anchoredPosition = new Vector2(-20, -2);
            rtBtnCopiar.sizeDelta = new Vector2(100, 36);

            Image imgBtn = btnCopiarGo.GetComponent<Image>();
            imgBtn.color = new Color(0.12f, 0.32f, 0.48f, 1f);
            imgBtn.raycastTarget = true;

            Button btnCopiar = btnCopiarGo.GetComponent<Button>();
            ColorBlock cb = btnCopiar.colors;
            cb.normalColor = new Color(0.12f, 0.32f, 0.48f, 1f);
            cb.highlightedColor = new Color(0.18f, 0.48f, 0.72f, 1f);
            cb.pressedColor = new Color(0.08f, 0.22f, 0.35f, 1f);
            cb.selectedColor = new Color(0.12f, 0.32f, 0.48f, 1f);
            btnCopiar.colors = cb;
            btnCopiar.onClick.RemoveAllListeners();
            btnCopiar.onClick.AddListener(CopiarCodigoAlPortapapeles);

            GameObject textCopiarGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textCopiarGo.transform.SetParent(btnCopiarGo.transform, false);
            RectTransform rtTxtCopiar = textCopiarGo.GetComponent<RectTransform>();
            rtTxtCopiar.anchorMin = Vector2.zero;
            rtTxtCopiar.anchorMax = Vector2.one;
            rtTxtCopiar.offsetMin = Vector2.zero;
            rtTxtCopiar.offsetMax = Vector2.zero;
            btnCopiarText = textCopiarGo.GetComponent<TextMeshProUGUI>();
            if (textoCodigo != null) btnCopiarText.font = textoCodigo.font;
            btnCopiarText.text = "COPIAR";
            btnCopiarText.fontSize = 11.5f;
            btnCopiarText.fontStyle = FontStyles.Bold;
            btnCopiarText.alignment = TextAlignmentOptions.Center;
            btnCopiarText.color = Color.white;
            btnCopiarText.raycastTarget = false;
        }

        btnCopiarGo.transform.SetAsLastSibling(); // Siempre al frente para recibir clicks sin obstrucción
        btnCopiarText = btnCopiarGo.GetComponentInChildren<TextMeshProUGUI>();

        // 8. Reparentar y estilizar TextoJugadores
        if (textoJugadores != null)
        {
            textoJugadores.transform.SetParent(cardGo.transform, false);
            RectTransform rtJugadores = textoJugadores.GetComponent<RectTransform>();
            rtJugadores.anchorMin = new Vector2(0.5f, 1f);
            rtJugadores.anchorMax = new Vector2(0.5f, 1f);
            rtJugadores.pivot = new Vector2(0.5f, 1f);
            rtJugadores.anchoredPosition = new Vector2(0, -196);
            rtJugadores.sizeDelta = new Vector2(440, 26);
            textoJugadores.fontSize = 13f;
            textoJugadores.alignment = TextAlignmentOptions.Center;
            textoJugadores.color = Color.white;
        }

        // 9. Banner de espera para clientes
        Transform existingBanner = cardGo.transform.Find("BannerEsperando");
        if (existingBanner != null)
        {
            bannerEsperandoHost = existingBanner.gameObject;
        }
        else
        {
            bannerEsperandoHost = new GameObject("BannerEsperando", typeof(RectTransform), typeof(TextMeshProUGUI));
            bannerEsperandoHost.transform.SetParent(cardGo.transform, false);
            RectTransform rtBanner = bannerEsperandoHost.GetComponent<RectTransform>();
            rtBanner.anchorMin = new Vector2(0.5f, 0f);
            rtBanner.anchorMax = new Vector2(0.5f, 0f);
            rtBanner.pivot = new Vector2(0.5f, 0f);
            rtBanner.anchoredPosition = new Vector2(0, 56);
            rtBanner.sizeDelta = new Vector2(400, 32);
            TextMeshProUGUI tmpBanner = bannerEsperandoHost.GetComponent<TextMeshProUGUI>();
            if (textoCodigo != null) tmpBanner.font = textoCodigo.font;
            tmpBanner.text = "<b><color=#FBBF24>ESPERANDO A QUE EL HOST INICIE LA MISIÓN...</color></b>";
            tmpBanner.fontSize = 11.5f;
            tmpBanner.alignment = TextAlignmentOptions.Center;
            bannerEsperandoHost.SetActive(false);
        }

        // 10. Estilizar botón Iniciar Partida (Verde táctico)
        if (botonIniciarPartida != null)
        {
            botonIniciarPartida.transform.SetParent(cardGo.transform, false);
            RectTransform rtPlay = botonIniciarPartida.GetComponent<RectTransform>();
            rtPlay.anchorMin = new Vector2(0.5f, 0f);
            rtPlay.anchorMax = new Vector2(0.5f, 0f);
            rtPlay.pivot = new Vector2(0.5f, 0f);
            rtPlay.anchoredPosition = new Vector2(0, 54);
            rtPlay.sizeDelta = new Vector2(280, 36);

            Image imgPlay = botonIniciarPartida.GetComponent<Image>();
            if (imgPlay != null) imgPlay.color = new Color(0.14f, 0.65f, 0.38f, 1f);

            Button btnPlay = botonIniciarPartida.GetComponent<Button>();
            if (btnPlay != null)
            {
                ColorBlock cb = btnPlay.colors;
                cb.normalColor = new Color(0.14f, 0.65f, 0.38f, 1f);
                cb.highlightedColor = new Color(0.18f, 0.78f, 0.45f, 1f);
                cb.pressedColor = new Color(0.10f, 0.50f, 0.28f, 1f);
                btnPlay.colors = cb;
            }

            TMP_Text tmpPlay = botonIniciarPartida.GetComponentInChildren<TMP_Text>();
            if (tmpPlay != null)
            {
                tmpPlay.text = "INICIAR PARTIDA";
                tmpPlay.fontSize = 13f;
                tmpPlay.fontStyle = FontStyles.Bold;
                tmpPlay.color = Color.white;
                tmpPlay.alignment = TextAlignmentOptions.Center;
            }
        }

        // 11. Encontrar y estilizar Boton Salir
        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true);
        foreach (var b in allButtons)
        {
            if (b.gameObject != botonIniciarPartida && b.name != "BtnCopiar")
            {
                botonSalirRef = b;
                break;
            }
        }

        if (botonSalirRef != null)
        {
            botonSalirRef.transform.SetParent(cardGo.transform, false);
            RectTransform rtSalir = botonSalirRef.GetComponent<RectTransform>();
            rtSalir.anchorMin = new Vector2(0.5f, 0f);
            rtSalir.anchorMax = new Vector2(0.5f, 0f);
            rtSalir.pivot = new Vector2(0.5f, 0f);
            rtSalir.anchoredPosition = new Vector2(0, 16);
            rtSalir.sizeDelta = new Vector2(280, 30);

            Image imgSalir = botonSalirRef.GetComponent<Image>();
            if (imgSalir != null) imgSalir.color = new Color(0.24f, 0.12f, 0.14f, 0.95f);

            ColorBlock cb = botonSalirRef.colors;
            cb.normalColor = new Color(0.24f, 0.12f, 0.14f, 0.95f);
            cb.highlightedColor = new Color(0.45f, 0.18f, 0.20f, 1f);
            cb.pressedColor = new Color(0.15f, 0.08f, 0.09f, 1f);
            botonSalirRef.colors = cb;

            TMP_Text tmpSalir = botonSalirRef.GetComponentInChildren<TMP_Text>();
            if (tmpSalir != null)
            {
                tmpSalir.text = "SALIR DE LA SALA";
                tmpSalir.fontSize = 11.5f;
                tmpSalir.fontStyle = FontStyles.Bold;
                tmpSalir.color = new Color(0.95f, 0.55f, 0.55f, 1f);
                tmpSalir.alignment = TextAlignmentOptions.Center;
            }
        }
    }

    private Coroutine feedbackCoroutine;

    private void CopiarCodigoAlPortapapeles()
    {
        Debug.Log("[LobbyUI] Click en botón Copiar Código");

        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        string codigo = "";

        if (LobbyManager.Instance != null)
        {
            codigo = LobbyManager.Instance.GetLobbyCode();
            if (string.IsNullOrEmpty(codigo))
            {
                codigo = LobbyManager.Instance.LobbyCode;
            }
        }

        if (string.IsNullOrEmpty(codigo) && !string.IsNullOrEmpty(RelayConnectionManager.CodigoPartidaActual))
        {
            codigo = RelayConnectionManager.CodigoPartidaActual;
        }

        if (string.IsNullOrEmpty(codigo) && textoCodigo != null)
        {
            string raw = textoCodigo.text;
            raw = System.Text.RegularExpressions.Regex.Replace(raw, "<.*?>", string.Empty);
            raw = raw.Replace("Código:", "").Replace("Codigo:", "").Replace("CÓDIGO:", "").Trim();
            if (!string.IsNullOrEmpty(raw) && raw != "-----")
            {
                codigo = raw;
            }
        }

        if (!string.IsNullOrEmpty(codigo))
        {
            GUIUtility.systemCopyBuffer = codigo.Trim().ToUpper();
            Debug.Log($"[LobbyUI] Código copiado al portapapeles con éxito: '{GUIUtility.systemCopyBuffer}'");
        }

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }
        feedbackCoroutine = StartCoroutine(FeedbackCopiadoRoutine());
    }

    private IEnumerator FeedbackCopiadoRoutine()
    {
        if (btnCopiarText != null)
        {
            btnCopiarText.text = "¡COPIADO!";
            btnCopiarText.color = new Color(0.35f, 1f, 0.65f, 1f);
        }

        yield return new WaitForSeconds(1.5f);

        if (btnCopiarText != null)
        {
            btnCopiarText.text = "COPIAR";
            btnCopiarText.color = Color.white;
        }
    }

    private void ActualizarUI()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("No se encontró LobbyManager.");
            return;
        }

        string codigoLobby = LobbyManager.Instance.GetLobbyCode();
        if (string.IsNullOrEmpty(codigoLobby)) codigoLobby = "-----";

        if (textoCodigo != null)
        {
            textoCodigo.text = $"<b>{codigoLobby}</b>";
        }

        int playerCount = LobbyManager.Instance.GetPlayerCount();
        if (textoJugadores != null)
        {
            textoJugadores.text = $"JUGADORES CONECTADOS:  <b><color=#4ADE80>{playerCount}</color></b>";
        }

        bool esHost = LobbyManager.Instance.IsHost();

        if (textoEstado != null)
        {
            if (esHost)
            {
                textoEstado.text = "<b><color=#4ADE80>● ANFITRIÓN (HOST)</color></b>  <color=#94A3B8>|  Sala lista para iniciar</color>";
            }
            else
            {
                textoEstado.text = "<b><color=#38BDF8>● JUGADOR CONECTADO</color></b>  <color=#94A3B8>|  Esperando al anfitrión</color>";
            }
        }

        if (botonIniciarPartida != null)
        {
            botonIniciarPartida.SetActive(esHost);
        }

        if (bannerEsperandoHost != null)
        {
            bannerEsperandoHost.SetActive(!esHost);
        }
    }

    private void ComprobarCliente()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("CLIENTE - No existe LobbyManager.");
            return;
        }

        bool esHost = LobbyManager.Instance.IsHost();
        bool partidaIniciada = LobbyManager.Instance.PartidaIniciada;
        string codigoRelay = LobbyManager.Instance.GetCodigoRelay();

        if (esHost) return;
        if (!partidaIniciada) return;
        if (clienteConectandose) return;
        if (string.IsNullOrEmpty(codigoRelay)) return;

        clienteConectandose = true;

        Debug.Log("El Host inició la partida.");
        Debug.Log("Código Relay recibido: " + codigoRelay);

        relayManager.JoinRelay(codigoRelay);
    }

    public async void IniciarPartida()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        if (LobbyManager.Instance == null || !LobbyManager.Instance.IsHost())
            return;

        Debug.Log("EL HOST INICIÓ LA PARTIDA");

        string codigoRelay = await relayManager.CreateRelay();

        if (string.IsNullOrEmpty(codigoRelay))
        {
            Debug.LogError("No se pudo crear el Relay.");
            return;
        }

        await LobbyManager.Instance.GuardarCodigoRelay(codigoRelay);
        await LobbyManager.Instance.MarcarPartidaIniciada();

        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(
            "escenaPrincipal",
            LoadSceneMode.Single
        );
    }

    public async void SalirDeLaSala()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        Debug.Log("Saliendo de la sala...");

        if (LobbyManager.Instance != null)
        {
            await LobbyManager.Instance.SalirDelLobby();
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("menuPrincipal");
    }
}
