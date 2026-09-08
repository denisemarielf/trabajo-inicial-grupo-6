
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class TowerHealthTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;
    private TowerHealth towerHealth;

    [UnitySetUp]
    public IEnumerator SetUpJuego()
    {
        yield return CrearEntornoDePrueba();
        yield return IniciarPartidaComoHost();
        yield return ObtenerTowerHealth();
    }

    private IEnumerator CrearEntornoDePrueba()
    {
        yield return SceneManager.LoadSceneAsync(
            "menuPrincipal",
            LoadSceneMode.Single
        );

        yield return new WaitUntil(() => LobbyManager.Instance != null);

        lobbyManager = LobbyManager.Instance;

        Assert.IsNotNull(
            lobbyManager,
            "No se encontró el LobbyManager."
        );

        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        networkManager = NetworkManager.Singleton;

        Assert.IsNotNull(
            networkManager,
            "No se encontró el NetworkManager."
        );

        relayManager = Object.FindAnyObjectByType<RelayConnectionManager>();

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

    private IEnumerator IniciarPartidaComoHost()
    {
        lobbyManager.CrearLobby();

        yield return new WaitUntil(() => TieneLobbyActual());

        yield return new WaitUntil(
            () => SceneManager.GetActiveScene().name == "salaEspera"
        );

        var tareaRelay = relayManager.CreateRelay();

        yield return new WaitUntil(
            () => tareaRelay.IsCompleted
        );

        string codigoRelay = tareaRelay.Result;

        Assert.IsFalse(
            string.IsNullOrEmpty(codigoRelay),
            "No se pudo crear el código Relay."
        );

        var tareaGuardar =
            lobbyManager.GuardarCodigoRelay(codigoRelay);

        yield return new WaitUntil(
            () => tareaGuardar.IsCompleted
        );

        var tareaIniciar =
            lobbyManager.MarcarPartidaIniciada();

        yield return new WaitUntil(
            () => tareaIniciar.IsCompleted
        );

        bool iniciado = networkManager.StartHost();

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

    private IEnumerator ObtenerTowerHealth()
    {
        yield return new WaitUntil(() =>
            Object.FindAnyObjectByType<TowerHealth>() != null
        );

        towerHealth =
            Object.FindAnyObjectByType<TowerHealth>();

        Assert.IsNotNull(
            towerHealth,
            "No se encontró TowerHealth en la escena."
        );

        yield return new WaitUntil(() =>
            ObtenerVidaTorre() > 0
        );

        Assert.AreEqual(
            towerHealth.maxHealth,
            ObtenerVidaTorre()
        );
    }

    private float ObtenerVidaTorre()
    {
        var campo = typeof(TowerHealth).GetField(
            "currentHealth",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        Assert.IsNotNull(
            campo,
            "No se encontró currentHealth."
        );

        var networkVariable = campo.GetValue(towerHealth);

        var propiedadValue =
            networkVariable.GetType().GetProperty("Value");

        return (float)propiedadValue.GetValue(
            networkVariable
        );
    }

    [UnityTest]
    public IEnumerator TorreRecibeDanio_LaVidaDisminuyeEn10()
    {
        float vidaInicial = ObtenerVidaTorre();

        Assert.AreEqual(
            towerHealth.maxHealth,
            vidaInicial
        );

        float dano = 10f;

        towerHealth.TakeDamage(dano);

        yield return null;

        float vidaFinal = ObtenerVidaTorre();

        Assert.AreEqual(
            vidaInicial - dano,
            vidaFinal
        );
    }

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
        towerHealth = null;

        yield return null;
    }

    private bool TieneLobbyActual()
    {
        return lobbyManager != null &&
               lobbyManager.GetType()
                   .GetProperty("CurrentLobby")
                   ?.GetValue(lobbyManager) != null;
    }
}
