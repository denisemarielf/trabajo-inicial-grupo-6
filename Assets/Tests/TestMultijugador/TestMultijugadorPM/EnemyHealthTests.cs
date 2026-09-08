using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class EnemyHealthTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;
    private EnemyHealth enemyHealth;

    [UnitySetUp]
    public IEnumerator SetUpJuego()
    {
        yield return CrearEntornoDePrueba();
        yield return IniciarPartidaComoHost();
        yield return ObtenerEnemyHealth();
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

        relayManager = Object.FindFirstObjectByType<RelayConnectionManager>();

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

    private IEnumerator ObtenerEnemyHealth()
    {
        yield return new WaitUntil(() =>
            Object.FindFirstObjectByType<EnemyHealth>() != null
        );

        enemyHealth =
            Object.FindFirstObjectByType<EnemyHealth>();

        Assert.IsNotNull(
            enemyHealth,
            "No se encontró ningún EnemyHealth en la escena."
        );

        yield return new WaitUntil(() =>
            ObtenerVidaEnemigo() > 0
        );

        Assert.AreEqual(
            enemyHealth.maxHealth,
            ObtenerVidaEnemigo()
        );
    }

    private float ObtenerVidaEnemigo()
    {
        var campo = typeof(EnemyHealth).GetField(
            "currentHealth",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        Assert.IsNotNull(
            campo,
            "No se encontró currentHealth."
        );

        var networkVariable = campo.GetValue(enemyHealth);

        var propiedadValue =
            networkVariable.GetType().GetProperty("Value");

        return (float)propiedadValue.GetValue(
            networkVariable
        );
    }

    [UnityTest]
    public IEnumerator EnemigoRecibeDisparo_LaVidaDisminuye()
    {
        float vidaInicial = ObtenerVidaEnemigo();

        Assert.AreEqual(
            enemyHealth.maxHealth,
            vidaInicial
        );

        int dano = 20;

        enemyHealth.TakeDamage(dano);

        yield return null;

        float vidaFinal = ObtenerVidaEnemigo();

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
        enemyHealth = null;

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
