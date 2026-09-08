
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShootTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;

    private Shoot shoot;

    private Mouse mouse;
    private Keyboard keyboard;


    // =========================================================
    // SETUP JUEGO
    // =========================================================

    [UnitySetUp]
    public IEnumerator SetUpJuego()
    {
        mouse = Mouse.current;

        if (mouse == null)
            mouse = InputSystem.AddDevice<Mouse>();

        keyboard = Keyboard.current;

        if (keyboard == null)
            keyboard = InputSystem.AddDevice<Keyboard>();

        ResetearInput();

        yield return CrearEntornoDePrueba();

        yield return IniciarPartidaComoHost();

        yield return ObtenerShoot();
    }


    // =========================================================
    // CREAR ENTORNO DE PRUEBA
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
            Object.FindAnyObjectByType<RelayConnectionManager>();

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


    // =========================================================
    // OBTENER SHOOT DEL PLAYER REAL
    // =========================================================

    private IEnumerator ObtenerShoot()
    {
        yield return new WaitUntil(
            () => networkManager.LocalClient != null &&
                  networkManager.LocalClient.PlayerObject != null
        );

        GameObject player =
            networkManager.LocalClient.PlayerObject.gameObject;

        shoot = player.GetComponent<Shoot>();

        if (shoot == null)
            shoot = player.GetComponentInChildren<Shoot>(true);

        Assert.IsNotNull(
            shoot,
            "No se encontró ningún componente Shoot en el Player ni en sus hijos."
        );

        Assert.IsTrue(
            shoot.IsOwner,
            "El Shoot encontrado no pertenece al jugador local."
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
        shoot = null;
        mouse = null;
        keyboard = null;

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


    // =========================================================
    // CREAR ACCIONES
    // =========================================================

    private InputAction CrearAccionDeDisparo()
    {
        InputAction disparo =
            new InputAction(
                "Disparo",
                InputActionType.Button
            );

        disparo.AddBinding("<Mouse>/leftButton");

        disparo.performed += shoot.OnShoot;

        disparo.Enable();

        return disparo;
    }


    private InputAction CrearAccionDeRecarga()
    {
        InputAction recarga =
            new InputAction(
                "Recarga",
                InputActionType.Button
            );

        recarga.AddBinding("<Keyboard>/r");

        recarga.performed += shoot.OnReload;

        recarga.Enable();

        return recarga;
    }


    // =========================================================
    // INPUT - CLICK
    // =========================================================

    private void Click()
    {
        if (mouse == null)
            mouse = Mouse.current;

        Assert.IsNotNull(
            mouse,
            "No existe un dispositivo Mouse."
        );

        // Asegurar que el botón esté liberado
        InputSystem.QueueDeltaStateEvent(
            mouse.leftButton,
            (byte)0
        );

        InputSystem.Update();

        // Presionar el botón
        InputSystem.QueueDeltaStateEvent(
            mouse.leftButton,
            (byte)1
        );

        InputSystem.Update();
    }


    private void SoltarClick()
    {
        if (mouse == null)
            mouse = Mouse.current;

        Assert.IsNotNull(
            mouse,
            "No existe un dispositivo Mouse."
        );

        InputSystem.QueueDeltaStateEvent(
            mouse.leftButton,
            (byte)0
        );

        InputSystem.Update();
    }


    private void ResetearInput()
    {
        if (mouse == null)
            mouse = Mouse.current;

        if (mouse != null)
        {
            InputSystem.QueueDeltaStateEvent(
                mouse.leftButton,
                (byte)0
            );

            InputSystem.Update();
        }
    }


    // =========================================================
    // INPUT - RECARGAR
    // =========================================================

    private void IniciarRecarga()
    {
        System.Reflection.MethodInfo metodo =
            typeof(Shoot).GetMethod(
                "TryReload",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

        Assert.IsNotNull(
            metodo,
            "No se encontró el método TryReload."
        );

        metodo.Invoke(
            shoot,
            null
        );
    }


    // =========================================================
    // TEST A
    // =========================================================

    [UnityTest]
    public IEnumerator A_SinClic_NoSeDispara()
    {
        int municionInicial =
            shoot.CurrentAmmo;

        yield return null;

        Assert.AreEqual(
            municionInicial,
            shoot.CurrentAmmo
        );
    }


    // =========================================================
    // TEST B
    // =========================================================

    [UnityTest]
    public IEnumerator B_UnClic_SeDisparaUnaVezYDisminuyeLaMunicion()
    {
        int municionInicial =
            shoot.CurrentAmmo;

        InputAction disparo =
            CrearAccionDeDisparo();

        try
        {
            // Asegurar estado inicial
            SoltarClick();

            yield return null;

            // Realizar un click
            Click();

            yield return null;

            SoltarClick();

            Assert.AreEqual(
                municionInicial - 1,
                shoot.CurrentAmmo
            );
        }
        finally
        {
            disparo.performed -= shoot.OnShoot;
            disparo.Dispose();
        }
    }


    // =========================================================
    // TEST C
    // =========================================================

    [UnityTest]
    public IEnumerator C_DosClicsSeguidos_SeRespetaElTiempoDeDisparo()
    {
        int municionInicial =
            shoot.CurrentAmmo;

        InputAction disparo =
            CrearAccionDeDisparo();

        try
        {
            // Asegurar estado inicial
            SoltarClick();

            yield return null;

            // Primer disparo
            Click();

            yield return null;

            SoltarClick();

            int municionPrimerDisparo =
                shoot.CurrentAmmo;

            // Segundo disparo inmediato
            Click();

            yield return null;

            SoltarClick();

            int municionSegundoDisparo =
                shoot.CurrentAmmo;

            Assert.AreEqual(
                municionInicial - 1,
                municionPrimerDisparo
            );

            Assert.AreEqual(
                municionPrimerDisparo,
                municionSegundoDisparo
            );
        }
        finally
        {
            disparo.performed -= shoot.OnShoot;
            disparo.Dispose();
        }
    }


    // =========================================================
    // TEST D
    // =========================================================

    [UnityTest]
    public IEnumerator D_CargadorVacio_SeIniciaLaRecarga()
    {
        InputAction disparo =
            CrearAccionDeDisparo();

        try
        {
            // Vaciar el cargador respetando el shootRate
            while (shoot.CurrentAmmo > 0)
            {
                Click();

                yield return null;

                SoltarClick();

                yield return new WaitForSeconds(
                    shoot.shootRate + 0.05f
                );
            }

            Assert.AreEqual(
                0,
                shoot.CurrentAmmo
            );

            // Intentar disparar con cargador vacío
            Click();

            yield return null;

            SoltarClick();

            Assert.IsTrue(
                shoot.IsReloading,
                "La recarga no se inició con el cargador vacío."
            );
        }
        finally
        {
            disparo.performed -= shoot.OnShoot;
            disparo.Dispose();
        }
    }


    // =========================================================
    // TEST E
    // =========================================================

    [UnityTest]
    public IEnumerator E_Recarga_NoSuperaElLimiteDelCargador()
    {
        InputAction disparo =
            CrearAccionDeDisparo();

        try
        {
            // Realizar un disparo
            Click();

            yield return null;

            SoltarClick();
        }
        finally
        {
            disparo.performed -= shoot.OnShoot;
            disparo.Dispose();
        }

        // Crear acción de recarga
        InputAction recarga =
            CrearAccionDeRecarga();

        try
        {
            IniciarRecarga();

            yield return new WaitForSeconds(
                shoot.reloadTime + 0.1f
            );

            Assert.LessOrEqual(
                shoot.CurrentAmmo,
                shoot.magazineSize
            );

            Assert.AreEqual(
                shoot.magazineSize,
                shoot.CurrentAmmo
            );
        }
        finally
        {
            recarga.performed -= shoot.OnReload;
            recarga.Dispose();
        }
    }
}
