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
        {
            Debug.LogError("NetworkManager es NULL");
            return;
        }

        Debug.Log(
            "ESTADO NETCODE - IsHost: " +
            NetworkManager.Singleton.IsHost +
            " | IsClient: " +
            NetworkManager.Singleton.IsClient +
            " | IsListening: " +
            NetworkManager.Singleton.IsListening
        );

        // Si soy cliente, significa que perdí la conexión con el Host.
        if (!NetworkManager.Singleton.IsHost)
        {
            Debug.Log("EL HOST SE DESCONECTÓ");

            StartCoroutine(VolverAlMenu());

            return;
        }

        // Si soy el Host, significa que se desconectó otro jugador.
        if (disconnectText != null)
        {
            disconnectText.gameObject.SetActive(true);
            disconnectText.text = "El otro jugador se ha desconectado.";
        }
    }

    private System.Collections.IEnumerator VolverAlMenu()
    {
        Debug.Log("VOLVER AL MENU - INICIANDO");

        yield return null;

        if (NetworkManager.Singleton != null)
        {
            Debug.Log("VOLVER AL MENU - HACIENDO SHUTDOWN");

            NetworkManager.Singleton.Shutdown();
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log("VOLVER AL MENU - CARGANDO ESCENA");

        SceneManager.LoadScene("menuPrincipal");
    }
}