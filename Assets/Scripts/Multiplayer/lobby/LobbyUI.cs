
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_Text textoCodigo;
    [SerializeField] private TMP_Text textoJugadores;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private GameObject botonIniciarPartida;
    [SerializeField] private RelayConnectionManager relayManager;

    private float tiempoActualizacion = 0f;
    private float intervaloActualizacion = 0.5f;
    private bool clienteConectandose = false;


private void Start()
{
    Debug.Log("LOBBY UI INICIADO");

    Debug.Log(
        "LobbyManager existe: " +
        (LobbyManager.Instance != null)
    );

    if (LobbyManager.Instance != null)
    {
        Debug.Log(
            "LobbyCode desde Start: " +
            LobbyManager.Instance.GetLobbyCode()
        );

        Debug.Log(
            "Cantidad desde Start: " +
            LobbyManager.Instance.GetPlayerCount()
        );

        Debug.Log(
            "Es Host desde Start: " +
            LobbyManager.Instance.IsHost()
        );

        // PRUEBA DE DIAGNÓSTICO
        Debug.Log(
            "LOBBY UI - INSTANCE ID: " +
            LobbyManager.Instance.GetEntityId()
        );

        Debug.Log(
            "LOBBY UI - INSTANCE: " +
            LobbyManager.Instance.gameObject.name
        );
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
    private void ActualizarUI()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogError("No se encontró LobbyManager.");
            return;
        }

        textoCodigo.text =
            "Código: " + LobbyManager.Instance.GetLobbyCode();

        Debug.Log(
            "UI actualizada - Código mostrado: " +
            LobbyManager.Instance.GetLobbyCode()
        );

        textoJugadores.text =
            "Jugadores: " + LobbyManager.Instance.GetPlayerCount();

        bool esHost = LobbyManager.Instance.IsHost();

        if (esHost)
        {
            textoEstado.text = "Sala de espera del Host";
        }
        else
        {
            textoEstado.text = "Esperando al Host...";
        }

        botonIniciarPartida.SetActive(esHost);
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

    Debug.Log(
        "CLIENTE - ComprobarCliente | " +
        "EsHost: " + esHost +
        " | PartidaIniciada: " + partidaIniciada +
        " | CodigoRelay: " + codigoRelay +
        " | Conectandose: " + clienteConectandose
    );

    if (esHost)
    {
        Debug.Log("CLIENTE - Se detiene porque IsHost() devuelve TRUE.");
        return;
    }

    if (!partidaIniciada)
    {
        Debug.Log("CLIENTE - Se detiene porque PartidaIniciada es FALSE.");
        return;
    }

    if (clienteConectandose)
    {
        Debug.Log("CLIENTE - Ya está intentando conectarse.");
        return;
    }

    if (string.IsNullOrEmpty(codigoRelay))
    {
        Debug.Log("CLIENTE - Se detiene porque no tiene CodigoRelay.");
        return;
    }

    clienteConectandose = true;

    Debug.Log("El Host inició la partida.");
    Debug.Log("Código Relay recibido: " + codigoRelay);

    relayManager.JoinRelay(codigoRelay);
}
    public async void IniciarPartida()
    {
        if (!LobbyManager.Instance.IsHost())
            return;

        Debug.Log("EL HOST INICIÓ LA PARTIDA");

        string codigoRelay =
            await relayManager.CreateRelay();

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
        Debug.Log("Saliendo de la sala...");

        if (LobbyManager.Instance != null)
        {
            await LobbyManager.Instance.SalirDelLobby();
        }

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("menuPrincipal");
    }
}
