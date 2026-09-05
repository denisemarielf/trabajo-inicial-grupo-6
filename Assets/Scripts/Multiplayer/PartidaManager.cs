using UnityEngine;

public class PartidaManager : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("PARTIDA MANAGER INICIADO");
        
        if (LobbyManager.Instance != null)
        {
            Debug.Log("LIMPIANDO LOBBY DESDE PARTIDA MANAGER");
            LobbyManager.Instance.LimpiarLobbyLocal();
        }
        else
        {
            Debug.LogError("NO SE ENCONTRÓ LOBBY MANAGER");
        }
    }
}