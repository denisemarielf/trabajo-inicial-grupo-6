using Unity.Netcode;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        Camera[] allCameras = GetComponentsInChildren<Camera>(true);
        AudioListener listener = GetComponentInChildren<AudioListener>();

        foreach (Camera cam in allCameras)
        {
            cam.enabled = IsOwner;
        }

        if (listener != null)
            listener.enabled = IsOwner;
    }
}