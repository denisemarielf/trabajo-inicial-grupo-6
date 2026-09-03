
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
    private RelayConnectionManager relayManager;

    private IEnumerator CrearEntornoDePrueba()
    {
        // Cargar la escena que contiene el NetworkManager
        // y el RelayConnectionManager.
        yield return SceneManager.LoadSceneAsync(
            "menuPrincipal",
            LoadSceneMode.Single
        );

        // Obtener NetworkManager
        networkManager = NetworkManager.Singleton;

        Assert.IsNotNull(
            networkManager,
            "No se encontró el NetworkManager."
        );

        // Obtener RelayConnectionManager
        relayManager =
            Object.FindFirstObjectByType<RelayConnectionManager>();

        Assert.IsNotNull(
            relayManager,
            "No se encontró el RelayConnectionManager."
        );

        // Esperar a que Unity Services esté inicializado.
        yield return new WaitUntil(
            () => UnityServices.State ==
                  ServicesInitializationState.Initialized
        );

        // Esperar a que el usuario esté autenticado.
        yield return new WaitUntil(
            () => AuthenticationService.Instance.IsSignedIn
        );
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return CrearEntornoDePrueba();
    }


    [UnityTearDown]
    public IEnumerator TearDown()
    {
        // Apagar Netcode.
        if (networkManager != null &&
            networkManager.IsListening)
        {
            networkManager.Shutdown();

            yield return new WaitUntil(
                () => !networkManager.IsListening
            );
        }

        // Cerrar sesión para que el siguiente test
        // pueda volver a autenticarse.
        if (AuthenticationService.Instance != null &&
            AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        networkManager = null;
        relayManager = null;

        yield return null;
    }
    private IEnumerator IniciarPartidaComoHost()
    {
        relayManager.CreateRelay();

        // Esperar a que el Host empiece a escuchar.
        yield return new WaitUntil(
            () => networkManager.IsListening
        );

        // Esperar a que Netcode cree el Player del Host.
        yield return new WaitUntil(
            () => networkManager.LocalClient.PlayerObject != null
        );
    }


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
    [UnityTest]
public IEnumerator SeIniciaPartida_CargaCorrectamenteElMapa()
{
    yield return IniciarPartidaComoHost();

    // Esperamos a que se cargue la escena principal
    yield return new WaitUntil(
        () => SceneManager.GetActiveScene().name == "escenaPrincipal"
    );

    // Verificamos que los elementos esenciales del mapa existan
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
public IEnumerator SeCreaPartida_SeGeneraCodigoDeUnion()
{
    yield return IniciarPartidaComoHost();

    Assert.IsFalse(
        string.IsNullOrEmpty(RelayConnectionManager.CodigoPartidaActual),
        "No se generó un código de partida."
    );
}

[UnityTest]
public IEnumerator CodigoInvalido_NoSeConectaComoCliente()
{
    bool intentoFinalizado = false;

    System.Action callback = () =>
    {
        intentoFinalizado = true;
    };

    relayManager.OnConnectionAttemptFinished += callback;

    // El formato es válido para Relay, pero el código no corresponde
    // a ninguna partida existente.
    string codigoInvalido = "BCDFGH";

    LogAssert.Expect(
        LogType.Error,
        new System.Text.RegularExpressions.Regex(
            ".*RelayServiceException.*"
        )
    );

    relayManager.JoinRelay(codigoInvalido);

    yield return new WaitUntil(
        () => intentoFinalizado
    );

    Assert.IsFalse(
        networkManager.IsListening,
        "El cliente no debería conectarse con un código inexistente."
    );

    Assert.IsFalse(
        networkManager.IsClient,
        "El NetworkManager no debería quedar funcionando como Client."
    );

    relayManager.OnConnectionAttemptFinished -= callback;
}
[UnityTest]
public IEnumerator CodigoVacio_NoSeConectaComoCliente()
{
    bool intentoFinalizado = false;

    System.Action callback = () =>
    {
        intentoFinalizado = true;
    };

    relayManager.OnConnectionAttemptFinished += callback;

    // Esperamos que se produzca un error al intentar
    // conectarse sin ingresar un código.
    LogAssert.Expect(
        LogType.Error,
        new System.Text.RegularExpressions.Regex(".*")
    );

    relayManager.JoinRelay("");

    yield return new WaitUntil(
        () => intentoFinalizado
    );

    Assert.IsFalse(
        networkManager.IsListening,
        "No debería iniciarse una conexión con un código vacío."
    );

    Assert.IsFalse(
        networkManager.IsClient,
        "El NetworkManager no debería funcionar como Client."
    );

    relayManager.OnConnectionAttemptFinished -= callback;
}
}