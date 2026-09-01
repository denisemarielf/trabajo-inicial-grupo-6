using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RelayConnectionManager : MonoBehaviour
{
    public static string CodigoPartidaActual = "";
    public TMP_Text statusText;
    public TMP_Text codigoPartidaText;
    private string joinCodeInput = "";
    private string statusMessage = "";

    private bool isConnecting = false; // <-- nuevo
    public event Action OnConnectionAttemptFinished;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        if (isConnecting || NetworkManager.Singleton.IsListening) return; // <-- guarda
        isConnecting = true;

        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

            if (codigoPartidaText != null) codigoPartidaText.text = "Codigo de partida: " + joinCode;
            CodigoPartidaActual = joinCode;
            Debug.Log("Codigo de partida: " + joinCode);

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene("escenaPrincipal", LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            if (statusText != null) statusText.text = "Error al crear partida: " + e.Message;
            Debug.LogError(e);
        }
        finally
        {
            isConnecting = false;
            OnConnectionAttemptFinished?.Invoke();
        }
    }

    public async void JoinRelay(string joinCode)
    {
        if (isConnecting || NetworkManager.Singleton.IsListening) return; // <-- guarda
        isConnecting = true;

        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            statusMessage = "Error al unirse: " + e.Message;
            Debug.LogError(e);
        }
        finally
        {
            isConnecting = false;
            OnConnectionAttemptFinished?.Invoke();
        }
    }
}