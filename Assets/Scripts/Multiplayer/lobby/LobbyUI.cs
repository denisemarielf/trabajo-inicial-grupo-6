
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
        return;

    // El Host no ejecuta esta lógica
    if (LobbyManager.Instance.IsHost())
        return;

    // El Host todavía no inició
    if (!LobbyManager.Instance.PartidaIniciada)
        return;

    // Evita intentar conectarse varias veces
    if (clienteConectandose)
        return;

    string codigoRelay =
        LobbyManager.Instance.GetCodigoRelay();

    // Todavía no tenemos el código Relay
    if (string.IsNullOrEmpty(codigoRelay))
        return;

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
