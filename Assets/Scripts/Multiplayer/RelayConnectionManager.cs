using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;

public class RelayConnectionManager : MonoBehaviour
{
    public static string CodigoPartidaActual = "";

    public TMP_Text statusText;
    public TMP_Text codigoPartidaText;

    private bool isConnecting = false;

    public event Action OnConnectionAttemptFinished;

private void Start()
{
    Debug.Log("RelayConnectionManager listo.");
}

    public async Task<string> CreateRelay()
    {
        if (isConnecting || NetworkManager.Singleton.IsListening)
            return "";

        isConnecting = true;

        try
        {
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(2);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId
                );

            var transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls")
            );

            CodigoPartidaActual = joinCode;

            if (codigoPartidaText != null)
            {
                codigoPartidaText.text =
                    "Codigo de partida: " + joinCode;
            }

            Debug.Log("Código Relay creado: " + joinCode);

            return joinCode;
        }
        catch (System.Exception e)
        {
            if (statusText != null)
            {
                statusText.text =
                    "Error al crear partida: " + e.Message;
            }

            Debug.LogError(e);

            return "";
        }
        finally
        {
            isConnecting = false;
            OnConnectionAttemptFinished?.Invoke();
        }
    }

    public async void JoinRelay(string joinCode)
    {
        if (isConnecting || NetworkManager.Singleton.IsListening)
            return;

        isConnecting = true;

        try
        {
            JoinAllocation joinAllocation =
                await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(
                    joinAllocation,
                    "dtls"
                )
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al unirse a Relay: " + e);
        }
        finally
        {
            isConnecting = false;
            OnConnectionAttemptFinished?.Invoke();
        }
    }
}