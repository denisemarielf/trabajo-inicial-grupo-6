
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PowerUpTests
{
    private NetworkManager networkManager;
    private LobbyManager lobbyManager;
    private RelayConnectionManager relayManager;

    private PlayerHealth playerHealth;
    private PlayerMovementCC playerMovement;
    private WeaponSwitcher weaponSwitcher;
    private PowerUp powerUp;

    [UnitySetUp]
    public IEnumerator SetUpJuego()
    {
        yield return CrearEntornoDePrueba();
        yield return IniciarPartidaComoHost();
        yield return ObtenerJugador();
    }

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

    private IEnumerator ObtenerJugador()
    {
        yield return new WaitUntil(
            () => networkManager.LocalClient != null &&
                  networkManager.LocalClient.PlayerObject != null
        );

        GameObject player =
            networkManager.LocalClient.PlayerObject.gameObject;

        playerHealth =
            player.GetComponent<PlayerHealth>();

        playerMovement =
            player.GetComponent<PlayerMovementCC>();

        weaponSwitcher =
            player.GetComponentInChildren<WeaponSwitcher>();

        Assert.IsNotNull(
            playerHealth,
            "No se encontró PlayerHealth en el jugador."
        );

        Assert.IsNotNull(
            playerMovement,
            "No se encontró PlayerMovementCC en el jugador."
        );

        Assert.IsNotNull(
            weaponSwitcher,
            "No se encontró WeaponSwitcher en el jugador."
        );

        yield return new WaitUntil(
            () => playerHealth.GetCurrentHealth() > 0
        );
    }

    private IEnumerator ObtenerPowerUp(PowerUp.PowerUpType tipo)
    {
        powerUp = null;

        PowerUp[] powerUps =
            Object.FindObjectsByType<PowerUp>(
                FindObjectsInactive.Exclude
            );

        foreach (PowerUp p in powerUps)
        {
            if (p.type == tipo)
            {
                powerUp = p;
                break;
            }
        }

        Assert.IsNotNull(
            powerUp,
            "No se encontró un PowerUp del tipo " + tipo + " en la escena."
        );

        yield return null;
    }

    private void AplicarEfectoPowerUp()
    {
        MethodInfo metodo =
            typeof(PowerUp).GetMethod(
                "ApplyEffect",
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

        Assert.IsNotNull(
            metodo,
            "No se encontró el método ApplyEffect."
        );

        metodo.Invoke(
            powerUp,
            new object[]
            {
                playerHealth.gameObject
            }
        );
    }

    private float ObtenerMultiplicadorVelocidad()
    {
        var campo =
            typeof(PlayerMovementCC).GetField(
                "speedMultiplier",
                BindingFlags.NonPublic |
                BindingFlags.Instance
            );

        Assert.IsNotNull(
            campo,
            "No se encontró speedMultiplier."
        );

        var networkVariable =
            campo.GetValue(playerMovement);

        var propiedadValue =
            networkVariable.GetType().GetProperty("Value");

        return (float)propiedadValue.GetValue(
            networkVariable
        );
    }

    private Shoot ObtenerArmaPrincipal()
    {
        Assert.IsNotNull(
            weaponSwitcher.weapons,
            "El WeaponSwitcher no tiene armas asignadas."
        );

        Assert.Greater(
            weaponSwitcher.weapons.Length,
            0,
            "No hay armas configuradas."
        );

        GameObject arma =
            weaponSwitcher.weapons[0];

        Assert.IsNotNull(
            arma,
            "El arma principal no está asignada."
        );

        Shoot shoot =
            arma.GetComponent<Shoot>();

        Assert.IsNotNull(
            shoot,
            "El arma principal no tiene componente Shoot."
        );

        return shoot;
    }

    [UnityTest]
    public IEnumerator PowerUpHealth_RecuperaVidaDelJugador()
    {
        yield return ObtenerPowerUp(
            PowerUp.PowerUpType.Health
        );

        float vidaMaxima =
            playerHealth.maxHealth;

        playerHealth.TakeDamage(25f);

        yield return null;

        float vidaAntes =
            playerHealth.GetCurrentHealth();

        Assert.AreEqual(
            vidaMaxima - 25f,
            vidaAntes
        );

        AplicarEfectoPowerUp();

        yield return null;

        float vidaDespues =
            playerHealth.GetCurrentHealth();

        Assert.AreEqual(
            vidaMaxima,
            vidaDespues
        );
    }

    [UnityTest]
    public IEnumerator PowerUpSpeed_AumentaLaVelocidadTemporalmente()
    {
        yield return ObtenerPowerUp(
            PowerUp.PowerUpType.Speed
        );

        float velocidadInicial =
            ObtenerMultiplicadorVelocidad();

        Assert.AreEqual(
            1f,
            velocidadInicial
        );

        float multiplicadorEsperado =
            powerUp.amount;

        float duracion =
            powerUp.duration;

        AplicarEfectoPowerUp();

        yield return null;

        float velocidadAumentada =
            ObtenerMultiplicadorVelocidad();

        Assert.AreEqual(
            multiplicadorEsperado,
            velocidadAumentada
        );

        yield return new WaitForSeconds(
            duracion + 0.2f
        );

        float velocidadFinal =
            ObtenerMultiplicadorVelocidad();

        Assert.AreEqual(
            1f,
            velocidadFinal
        );
    }

    [UnityTest]
    public IEnumerator PowerUpAmmo_AumentaLaMunicionDeReserva()
    {
        yield return ObtenerPowerUp(
            PowerUp.PowerUpType.Ammo
        );

        Shoot shoot =
            ObtenerArmaPrincipal();

        int municionInicial =
            shoot.ReserveAmmo;

        int municionAgregada =
            (int)powerUp.amount;

        AplicarEfectoPowerUp();

        yield return null;

        int municionFinal =
            shoot.ReserveAmmo;

        Assert.AreEqual(
            municionInicial + municionAgregada,
            municionFinal
        );
    }

    [UnityTest]
    public IEnumerator PowerUpDamage_AumentaElDanioTemporalmente()
    {
        yield return ObtenerPowerUp(
            PowerUp.PowerUpType.Damage
        );

        Shoot shoot =
            ObtenerArmaPrincipal();

        int danioInicial =
            shoot.damageAmount;

        float multiplicador =
            powerUp.amount;

        float duracion =
            powerUp.duration;

        int danioEsperado =
            Mathf.RoundToInt(
                danioInicial * multiplicador
            );

        AplicarEfectoPowerUp();

        yield return null;

        int danioAumentado =
            shoot.damageAmount;

        Assert.AreEqual(
            danioEsperado,
            danioAumentado
        );

        yield return new WaitForSeconds(
            duracion + 0.2f
        );

        int danioFinal =
            shoot.damageAmount;

        Assert.AreEqual(
            danioInicial,
            danioFinal
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
        playerHealth = null;
        playerMovement = null;
        weaponSwitcher = null;
        powerUp = null;

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
