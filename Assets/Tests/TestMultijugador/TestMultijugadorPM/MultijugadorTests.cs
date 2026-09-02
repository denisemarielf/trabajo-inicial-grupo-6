
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


}