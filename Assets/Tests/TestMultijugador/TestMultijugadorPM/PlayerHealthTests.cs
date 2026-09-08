using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlayerHealthTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;
    private PlayerHealth playerHealth;


    // =========================================================
    // SETUP
    // =========================================================

    [UnitySetUp]
    public IEnumerator SetUpJuego()
    {
        yield return CrearEntornoDePrueba();

        yield return IniciarPartidaComoHost();

        yield return ObtenerPlayerHealth();
    }


    // =========================================================
    // CREAR ENTORNO
    // =========================================================

    private IEnumerator CrearEntornoDePrueba()
    {
        yield return SceneManager.LoadSceneAsync(
            "menuPrincipal",
            LoadSceneMode.Single
        );

        yield return new WaitUntil(
            () => LobbyManager.Instance != null
        );

        lobbyManager = LobbyManager.Instance;

        Assert.IsNotNull(
            lobbyManager,
            "No se encontró el LobbyManager."
        );

        yield return new WaitUntil(
            () => NetworkManager.Singleton != null
        );

        networkManager = NetworkManager.Singleton;

        Assert.IsNotNull(
            networkManager,
            "No se encontró el NetworkManager."
        );

        relayManager =
            Object.FindFirstObjectByType<RelayConnectionManager>();

        Assert.IsNotNull(
            relayManager,
            "No se encontró el RelayConnectionManager."
        );

        yield return new WaitUntil(
            () => UnityServices.State ==
                  ServicesInitializationState.Initialized
        );

        yield return new WaitUntil(
            () => AuthenticationService.Instance.IsSignedIn
        );
    }


    // =========================================================
    // CREAR PARTIDA COMO HOST
    // =========================================================

    private IEnumerator IniciarPartidaComoHost()
    {
        lobbyManager.CrearLobby();

        yield return new WaitUntil(
            () => TieneLobbyActual()
        );

        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name ==
                  "salaEspera"
        );

        var tareaRelay =
            relayManager.CreateRelay();

        yield return new WaitUntil(
            () => tareaRelay.IsCompleted
        );

        string codigoRelay =
            tareaRelay.Result;

        Assert.IsFalse(
            string.IsNullOrEmpty(codigoRelay),
            "No se pudo crear el código Relay."
        );

        var tareaGuardar =
            lobbyManager.GuardarCodigoRelay(
                codigoRelay
            );

        yield return new WaitUntil(
            () => tareaGuardar.IsCompleted
        );

        var tareaIniciar =
            lobbyManager.MarcarPartidaIniciada();

        yield return new WaitUntil(
            () => tareaIniciar.IsCompleted
        );

        bool iniciado =
            networkManager.StartHost();

        Assert.IsTrue(
            iniciado,
            "No se pudo iniciar el Host."
        );

        yield return new WaitUntil(
            () => networkManager.IsListening
        );

        networkManager.SceneManager.LoadScene(
            "escenaPrincipal",
            LoadSceneMode.Single
        );

        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name ==
                  "escenaPrincipal"
        );

        yield return new WaitUntil(
            () => networkManager.LocalClient != null &&
                  networkManager.LocalClient.PlayerObject != null
        );
    }


    // =========================================================
    // OBTENER PLAYER HEALTH
    // =========================================================

    private IEnumerator ObtenerPlayerHealth()
    {
        yield return new WaitUntil(
            () => networkManager.LocalClient != null &&
                  networkManager.LocalClient.PlayerObject != null
        );

        GameObject player =
            networkManager.LocalClient.PlayerObject.gameObject;

        playerHealth =
            player.GetComponent<PlayerHealth>();

        Assert.IsNotNull(
            playerHealth,
            "No se encontró PlayerHealth en el Player."
        );

        yield return new WaitUntil(
            () => playerHealth.GetCurrentHealth() > 0
        );
    }


    // =========================================================
    // TEST
    // =========================================================

    [UnityTest]
    public IEnumerator EnemigoAtacaJugador_LaVidaDisminuye()
    {
        float vidaInicial =
            playerHealth.GetCurrentHealth();

        Assert.AreEqual(
            playerHealth.maxHealth,
            vidaInicial
        );

        float dano = 10f;

        playerHealth.TakeDamage(dano);

        yield return null;

        float vidaFinal =
            playerHealth.GetCurrentHealth();

        Assert.AreEqual(
            vidaInicial - dano,
            vidaFinal
        );
    }


    // =========================================================
    // TEARDOWN
    // =========================================================

    [UnityTearDown]
    public IEnumerator TearDownJuego()
    {
        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();

            yield return new WaitUntil(
                () => !networkManager.IsListening
            );
        }

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
        playerHealth = null;

        yield return null;
    }


    // =========================================================
    // LOBBY
    // =========================================================

    private bool TieneLobbyActual()
    {
        return lobbyManager != null &&
               lobbyManager.GetType()
                   .GetProperty("CurrentLobby")
                   ?.GetValue(lobbyManager) != null;
    }
}
