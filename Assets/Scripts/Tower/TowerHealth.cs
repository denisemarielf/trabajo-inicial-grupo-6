using System;
using Unity.Netcode;
using UnityEngine;

public class TowerHealth : NetworkBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 200f;

    // Se sincroniza sola: cualquiera puede LEERLA, pero solo el servidor puede ESCRIBIRLA.
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isDestroyed = false;

    [Header("--------Audio--------")]
    private AudioSource audioSource;
    public AudioClip destroyTowerSound;

    // El GameManager (u otros scripts) se suscriben a esto
    public static event Action OnTowerDestroyed;

    public override void OnNetworkSpawn()
    {
        audioSource = GetComponent<AudioSource>();

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
    }

    
    public void TakeDamage(float amount)
    {
        if (!IsServer) return; // autoridad exclusiva del servidor
        if (isDestroyed) return;

        currentHealth.Value -= amount;
        Debug.Log($"Torre recibió {amount} de daño. Vida actual: {currentHealth.Value}/{maxHealth}");

        if (currentHealth.Value <= 0)
        {
            DestroyTower();
        }
    }
    private void DestroyTower()
    {
        isDestroyed = true;
        Debug.Log("La torre fue destruida");
        PlayDestroySoundClientRpc();
        NotifyTowerDestroyedClientRpc();
    }

    [ClientRpc]
    private void PlayDestroySoundClientRpc()
    {
        if (destroyTowerSound != null)
            AudioSource.PlayClipAtPoint(destroyTowerSound, transform.position);
    }

    [ClientRpc]
    private void NotifyTowerDestroyedClientRpc()
    {
        OnTowerDestroyed?.Invoke();
    }



    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    public float GetHealthPercent()
    {
        return currentHealth.Value / maxHealth;
    }

    public float GetCurrentHealth()
    {
        return currentHealth.Value;
    }
}