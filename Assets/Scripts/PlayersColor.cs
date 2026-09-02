using Unity.Netcode;
using UnityEngine;

public class PlayerColorHandler : NetworkBehaviour
{
    public Renderer bodyRenderer; // Arrastrá acá específicamente el Renderer del CUERPO del jugador
    public Color[] coloresDisponibles = new Color[] { Color.blue, Color.red, Color.green, Color.yellow };

    public override void OnNetworkSpawn()
    {
        if (bodyRenderer == null)
        {
            Debug.LogWarning("PlayerColorHandler: bodyRenderer no esta asignado en el Inspector.");
            return;
        }

        int index = (int)(OwnerClientId % (ulong)coloresDisponibles.Length);
        bodyRenderer.material.color = coloresDisponibles[index];

        Debug.Log("Color asignado al jugador " + OwnerClientId + ": " + coloresDisponibles[index]);
    }
}