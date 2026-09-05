using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    private Lobby currentLobby;

    public Lobby CurrentLobby => currentLobby;

    public string LobbyCode { get; private set; }

    public bool PartidaIniciada { get; private set; } = false;

    private float tiempoActualizacion = 2f;
    private float tiempoTranscurrido = 0f;

    private bool lobbyActivo = false;

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            Debug.Log("Unity Services inicializado.");
            Debug.Log(
                "Player ID: " +
                AuthenticationService.Instance.PlayerId
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error inicializando Unity Services: " + e
            );
        }
    }

    private async void Update()
    {
        if (!lobbyActivo || currentLobby == null)
            return;

        tiempoTranscurrido += Time.deltaTime;

        if (tiempoTranscurrido >= tiempoActualizacion)
        {
            tiempoTranscurrido = 0f;

            await ActualizarLobby();
        }
    }

    private async Task ActualizarLobby()
    {
        try
        {
            if (!lobbyActivo || currentLobby == null)
                return;

            string lobbyId = currentLobby.Id;

            Lobby lobbyActualizado =
                await LobbyService.Instance.GetLobbyAsync(
                    lobbyId
                );

            if (!lobbyActivo)
                return;

            currentLobby = lobbyActualizado;
            

Debug.Log(
    "CLIENTE - PartidaIniciada recibido: " +
    PartidaIniciada
);

if (currentLobby.Data != null)
{
    foreach (var dato in currentLobby.Data)
    {
        Debug.Log(
            "CLIENTE - DATO LOBBY: " +
            dato.Key + " = " +
            dato.Value.Value
        );
    }
}
            Debug.Log(
                "Lobby actualizado. Jugadores: " +
                currentLobby.Players.Count
            );

            if (currentLobby.Data != null &&
                currentLobby.Data.ContainsKey("PartidaIniciada"))
            {
                PartidaIniciada =
                    currentLobby.Data["PartidaIniciada"].Value == "true";
            }
        }
        catch (System.Exception e)
        {
            if (!lobbyActivo)
                return;

            Debug.LogError(
                "Error actualizando Lobby: " + e
            );
        }
    }


public async void CrearLobby()
{
    try
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError(
                "El jugador no está autenticado."
            );
            return;
        }

        CreateLobbyOptions options =
            new CreateLobbyOptions
            {
                IsPrivate = false
            };

        currentLobby =
            await LobbyService.Instance.CreateLobbyAsync(
                "Partida",
                4,
                options
            );

        lobbyActivo = true;

        // PRUEBA DE DIAGNÓSTICO
        Debug.Log(
            "NUEVO LOBBY CREADO - ID: " +
            currentLobby.Id
        );

        Debug.Log(
            "NUEVO LOBBY CODE: " +
            currentLobby.LobbyCode
        );

        Debug.Log(
            "CREAR LOBBY - INSTANCE ID: " +
            GetEntityId()
        );

        Debug.Log(
            "CREAR LOBBY - INSTANCE: " +
            gameObject.name
        );

        LobbyCode = currentLobby.LobbyCode;

        Debug.Log("Lobby creado.");
        Debug.Log(
            "Código del Lobby: " + LobbyCode
        );

        SceneManager.LoadScene("salaEspera");
    }
    catch (System.Exception e)
    {
        Debug.LogError(
            "Error creando Lobby: " + e
        );
    }
}


    public async void UnirseLobby(string codigo)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogError(
                    "El jugador no está autenticado."
                );
                return;
            }

            currentLobby =
                await LobbyService.Instance.JoinLobbyByCodeAsync(
                    codigo
                );

            lobbyActivo = true;

            LobbyCode = currentLobby.LobbyCode;

            Debug.Log("Se unió al Lobby.");
            Debug.Log(
                "Código del Lobby: " + LobbyCode
            );

            SceneManager.LoadScene("salaEspera");
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error uniéndose al Lobby: " + e
            );
        }
    }

    public string GetLobbyCode()
    {
        return currentLobby != null
            ? currentLobby.LobbyCode
            : "";
    }

    public int GetPlayerCount()
    {
        return currentLobby != null
            ? currentLobby.Players.Count
            : 0;
    }

    public bool IsHost()
    {
        return currentLobby != null &&
               currentLobby.HostId ==
               AuthenticationService.Instance.PlayerId;
    }

    public async Task MarcarPartidaIniciada()
    {
        if (!IsHost())
            return;

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data =
                        new Dictionary<string, DataObject>
                        {
                            {
                                "PartidaIniciada",
                                new DataObject(
                                    DataObject.VisibilityOptions.Member,
                                    "true"
                                )
                            }
                        }
                }
            );

            PartidaIniciada = true;

            Debug.Log(
                "Lobby marcado como partida iniciada."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error marcando partida iniciada: " + e
            );
        }
    }

    public async Task GuardarCodigoRelay(string codigoRelay)
    {
        if (!IsHost())
            return;

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(
                currentLobby.Id,
                new UpdateLobbyOptions
                {
                    Data =
                        new Dictionary<string, DataObject>
                        {
                            {
                                "CodigoRelay",
                                new DataObject(
                                    DataObject.VisibilityOptions.Member,
                                    codigoRelay
                                )
                            }
                        }
                }
            );

            Debug.Log(
                "Código Relay guardado en Lobby: " +
                codigoRelay
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error guardando código Relay: " + e
            );
        }
    }

    public string GetCodigoRelay()
    {
        if (currentLobby != null &&
            currentLobby.Data != null &&
            currentLobby.Data.ContainsKey("CodigoRelay"))
        {
            return currentLobby.Data["CodigoRelay"].Value;
        }

        return "";
    }

    public void LimpiarLobbyLocal()
    {
        lobbyActivo = false;

        currentLobby = null;
        LobbyCode = "";
        PartidaIniciada = false;

        Debug.Log("Lobby limpiado localmente.");
    }

    public async Task SalirDelLobby()
    {
        if (currentLobby == null)
            return;

        try
        {
            string playerId =
                AuthenticationService.Instance.PlayerId;

            if (currentLobby.HostId == playerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(
                    currentLobby.Id
                );

                Debug.Log("Lobby eliminado por el Host.");
            }
            else
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    currentLobby.Id,
                    playerId
                );

                Debug.Log("Jugador eliminado del Lobby.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Error saliendo del Lobby: " + e
            );
        }
        finally
        {
            lobbyActivo = false;

            currentLobby = null;
            LobbyCode = "";
            PartidaIniciada = false;
        }
    }
}