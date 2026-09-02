using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro;

public class DisconnectManger : MonoBehaviour
{
    public TMP_Text disconnectText;

    private void Start()
    {
        if (disconnectText != null)
        {
            disconnectText.gameObject.SetActive(false);
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("Cliente desconectado: " + clientId);

        if (NetworkManager.Singleton == null)
            return;

        Debug.Log("Mi ClientId: " + NetworkManager.Singleton.LocalClientId);

        // Si YO soy el que se está desconectando, no hago nada
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;

        // Si soy cliente y se desconectó el Host
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("El Host se desconectó. Volviendo al menú.");

            NetworkManager.Singleton.Shutdown();

            SceneManager.LoadScene("menuPrincipal");

            return;
        }

        // Si soy el Host y se desconectó el otro jugador
        if (disconnectText != null)
        {
            disconnectText.gameObject.SetActive(true);
            disconnectText.text = "El otro jugador se ha desconectado.";
        }
    }
}