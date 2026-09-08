
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class MultijugadorTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;

    private bool TieneLobbyActual()
    {
        return lobbyManager != null &&
               lobbyManager.GetType().GetProperty("CurrentLobby")?.GetValue(lobbyManager) != null;
    }

    // =========================================================
    // SETUP
    // =========================================================

    private IEnumerator CrearEntornoDePrueba()
    {
        // Cargar el menú principal.
        yield return SceneManager.LoadSceneAsync(
            "menuPrincipal",
            LoadSceneMode.Single
        );

        // Esperar a que exista el LobbyManager.
        yield return new WaitUntil(
            () => LobbyManager.Instance != null
        );

        lobbyManager = LobbyManager.Instance;

        Assert.IsNotNull(
            lobbyManager,
            "No se encontró el LobbyManager."
        );

        // Obtener NetworkManager.
        yield return new WaitUntil(
            () => NetworkManager.Singleton != null
        );

        networkManager = NetworkManager.Singleton;

        Assert.IsNotNull(
            networkManager,
            "No se encontró el NetworkManager."
        );

        // Obtener RelayConnectionManager.
        relayManager =
            Object.FindFirstObjectByType<RelayConnectionManager>();

        Assert.IsNotNull(
            relayManager,
            "No se encontró el RelayConnectionManager."
        );

        // Esperar inicialización de Unity Services.
        yield return new WaitUntil(
            () => UnityServices.State ==
                  ServicesInitializationState.Initialized
        );

        // Esperar autenticación.
        yield return new WaitUntil(
            () => AuthenticationService.Instance.IsSignedIn
        );
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return CrearEntornoDePrueba();
    }


    // =========================================================
    // TEARDOWN
    // =========================================================

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Apagar Netcode si estaba funcionando.
        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();

            yield return new WaitUntil(
                () => !networkManager.IsListening
            );
        }

        // Salir del Lobby si existe.
        if (TieneLobbyActual())
        {
            var tareaSalir =
                lobbyManager.SalirDelLobby();

            yield return new WaitUntil(
                () => tareaSalir.IsCompleted
            );
        }
        else if (lobbyManager != null)
        {
            lobbyManager.LimpiarLobbyLocal();
        }

        networkManager = null;
        relayManager = null;
        lobbyManager = null;

        yield return null;
    }


    // =========================================================
    // MÉTODO AUXILIAR: CREAR PARTIDA COMO HOST
    // =========================================================

    private IEnumerator IniciarPartidaComoHost()
    {
        // Crear el Lobby.
        lobbyManager.CrearLobby();

        // Esperar a que se cree.
        yield return new WaitUntil(
            () => TieneLobbyActual()
        );

        // Esperar a que se cargue la sala de espera.
        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name ==
                  "salaEspera"
        );

        // Crear Relay.
        var tareaRelay = relayManager.CreateRelay();

        yield return new WaitUntil(
            () => tareaRelay.IsCompleted
        );

        string codigoRelay = tareaRelay.Result;

        Assert.IsFalse(
            string.IsNullOrEmpty(codigoRelay),
            "No se pudo crear el código Relay."
        );

        // Guardar código Relay en el Lobby.
        var tareaGuardar =
            lobbyManager.GuardarCodigoRelay(codigoRelay);

        yield return new WaitUntil(
            () => tareaGuardar.IsCompleted
        );

        // Marcar partida como iniciada.
        var tareaIniciar =
            lobbyManager.MarcarPartidaIniciada();

        yield return new WaitUntil(
            () => tareaIniciar.IsCompleted
        );

        // Iniciar Host.
        networkManager.StartHost();

        yield return new WaitUntil(
            () => networkManager.IsListening
        );

        // Cargar escena principal mediante Netcode.
        networkManager.SceneManager.LoadScene(
            "escenaPrincipal",
            LoadSceneMode.Single
        );

        // Esperar escena principal.
        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name ==
                  "escenaPrincipal"
        );

        // Esperar Player del Host.
        yield return new WaitUntil(
            () => networkManager.LocalClient != null &&
                  networkManager.LocalClient.PlayerObject != null
        );
    }


    // =========================================================
    // TEST 1 - CREAR LOBBY
    // =========================================================

    [UnityTest]
    public IEnumerator CrearPartida_SeIngresaALaSalaDeEspera()
    {
        lobbyManager.CrearLobby();

        yield return new WaitUntil(
            () => TieneLobbyActual()
        );

        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name ==
                  "salaEspera"
        );

        Assert.IsNotNull(
            lobbyManager.GetType().GetProperty("CurrentLobby")?.GetValue(lobbyManager),
            "No se creó el Lobby."
        );

        Assert.AreEqual(
            "salaEspera",
            SceneManager.GetActiveScene().name,
            "No se cargó la sala de espera."
        );

        Assert.IsFalse(
            string.IsNullOrEmpty(lobbyManager.GetLobbyCode()),
            "El Lobby no tiene código."
        );
    }


    // =========================================================
    // TEST 2 - CÓDIGO INVÁLIDO
    // =========================================================

    [UnityTest]
    public IEnumerator CodigoInvalido_NoSeIngresaAlLobby()
    {
        bool intentoFinalizado = false;

        System.Action callback = () =>
        {
            intentoFinalizado = true;
        };

        lobbyManager.OnLobbyJoinAttemptFinished += callback;

        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex(
                ".*Error uniéndose al Lobby.*"
            )
        );

        lobbyManager.UnirseLobby("BCDFGH");

        yield return new WaitUntil(
            () => intentoFinalizado
        );

        Assert.IsNull(
            lobbyManager.GetType().GetProperty("CurrentLobby")?.GetValue(lobbyManager),
            "No debería existir un Lobby con un código inválido."
        );

        Assert.AreEqual(
            "menuPrincipal",
            SceneManager.GetActiveScene().name,
            "No debería acceder a la sala de espera."
        );

        lobbyManager.OnLobbyJoinAttemptFinished -= callback;
    }


    // =========================================================
    // TEST 3 - CÓDIGO VACÍO
    // =========================================================

    [UnityTest]
    public IEnumerator CodigoVacio_NoSeIngresaAlLobby()
    {
        bool intentoFinalizado = false;

        System.Action callback = () =>
        {
            intentoFinalizado = true;
        };

        lobbyManager.OnLobbyJoinAttemptFinished += callback;

        LogAssert.Expect(
            LogType.Error,
            new System.Text.RegularExpressions.Regex(
                ".*Error uniéndose al Lobby.*"
            )
        );

        lobbyManager.UnirseLobby("");

        yield return new WaitUntil(
            () => intentoFinalizado
        );

        Assert.IsNull(
            lobbyManager.GetType().GetProperty("CurrentLobby")?.GetValue(lobbyManager),
            "No debería existir un Lobby con un código vacío."
        );

        Assert.AreEqual(
            "menuPrincipal",
            SceneManager.GetActiveScene().name,
            "No debería acceder a la sala de espera."
        );

        lobbyManager.OnLobbyJoinAttemptFinished -= callback;
    }


    // =========================================================
    // TEST 4 - INICIO COMO HOST
    // =========================================================

    [UnityTest]
    public IEnumerator SeIniciaPartida_CreaJugadorHost()
    {
        yield return IniciarPartidaComoHost();

        Assert.IsTrue(
            networkManager.IsHost,
            "La partida no se inició como Host."
        );

        Assert.IsNotNull(
            networkManager.LocalClient.PlayerObject,
            "No se creó el Player del Host."
        );
    }


    // =========================================================
    // TEST 5 - SIN JUGADORES FANTASMA
    // =========================================================

    [UnityTest]
    public IEnumerator SoloHost_NoHayJugadoresFantasma()
    {
        yield return IniciarPartidaComoHost();

        Assert.IsTrue(
            networkManager.IsHost,
            "La partida no se inició como Host."
        );

        Assert.AreEqual(
            1,
            networkManager.ConnectedClientsList.Count,
            "Debe existir solamente un cliente conectado: el Host."
        );

        Assert.IsNotNull(
            networkManager.LocalClient.PlayerObject,
            "El Host no tiene un Player."
        );
    }


    // =========================================================
    // TEST 6 - CARGA DEL MAPA
    // =========================================================

    [UnityTest]
    public IEnumerator SeIniciaPartida_CargaCorrectamenteElMapa()
    {
        yield return IniciarPartidaComoHost();

        Assert.AreEqual(
            "escenaPrincipal",
            SceneManager.GetActiveScene().name,
            "No se cargó la escena principal."
        );

        Assert.IsNotNull(
            GameObject.Find("Terrain"),
            "No se encontró el Terrain en el mapa."
        );

        Assert.IsNotNull(
            GameObject.Find("Tower_a"),
            "No se encontró Tower_a en el mapa."
        );

        Assert.IsNotNull(
            GameObject.Find("object"),
            "No se encontró object en el mapa."
        );

        Assert.IsNotNull(
            GameObject.Find("Limites"),
            "No se encontraron los límites del mapa."
        );
    }


    [UnityTest]
    public IEnumerator SeCreaPartida_SeGeneraCodigoRelay()
    {
        yield return IniciarPartidaComoHost();

        Assert.IsFalse(
            string.IsNullOrEmpty(
                RelayConnectionManager.CodigoPartidaActual
            ),
            "No se generó un código Relay."
        );
    }
}