using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : NetworkBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 100f;

    [Header("--------Sonidos--------")]
    public AudioSource audioSource;
    public AudioClip damageSound;
    public AudioClip deathSound;


    // Sincronizada: todos los clientes leen la misma vida, solo el server la escribe.
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<bool> isDeadNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public static event Action<ulong> OnPlayerDied;

    private PlayerHealthUI healthUI;

    [Header("--------Derrota individual--------")]
    [SerializeField] private float returnToMenuDelay = 3f;
    [SerializeField] private string menuSceneName = "MainMenu";

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            StartCoroutine(RegisterWithGameManagerWhenReady());
        }

        currentHealth.OnValueChanged += HandleHealthChanged;

        // Solo el dueño necesita su propia barra en pantalla (mismo patrón que AmmoUI).
        if (IsOwner)
        {
            StartCoroutine(BuscarHealthUI());
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private System.Collections.IEnumerator RegisterWithGameManagerWhenReady()
    {
        while (GameManager.Instance == null)
            yield return null;

        GameManager.Instance.RegisterPlayer(OwnerClientId);
    }

    private System.Collections.IEnumerator BuscarHealthUI()
    {
        while (healthUI == null)
        {
            healthUI = FindAnyObjectByType<PlayerHealthUI>();
            if (healthUI == null) yield return null;
        }
        healthUI.UpdateHealthBar(currentHealth.Value, maxHealth);
    }

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        if (IsOwner && healthUI != null)
        {
            healthUI.UpdateHealthBar(newValue, maxHealth);
        }
    }

  
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        if (isDeadNet.Value) return;

        currentHealth.Value -= amount;

        PlayDamageSoundClientRpc(
          new ClientRpcParams
          {
              Send = new ClientRpcSendParams
              {
                  TargetClientIds = new[] { OwnerClientId }
              }
          }
      );

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        isDeadNet.Value = true;
        Debug.Log($"{gameObject.name} murió.");
        PlayDeathSoundClientRpc(
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            }
        );

       
        OnPlayerDied?.Invoke(OwnerClientId);

      
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        ShowPersonalLoseClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void ShowPersonalLoseClientRpc(ClientRpcParams rpcParams = default)
    {
    
        if (PersonalLosePanel.Instance != null)
        {
            PersonalLosePanel.Instance.Show();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No se encontro PersonalLosePanel.Instance en la escena.");
        }

        
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[PlayerHealth] Murió el host: se queda como servidor, sin Shutdown local.");
            return;
        }

        StartCoroutine(ReturnToMenuAfterDelay());
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown(); // se desconecta de la partida

        SceneManager.LoadScene("menuPrincipal"); // vuelve a SU menú local
    }


    [ClientRpc]
    private void PlayDamageSoundClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (audioSource != null && damageSound != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
    }

    [ClientRpc]
    private void PlayDeathSoundClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
    }
    public bool IsDead() => isDeadNet.Value;
    public float GetHealthPercent() => currentHealth.Value / maxHealth;
    public float GetCurrentHealth() => currentHealth.Value;
    public void Heal(float amount)
    {
        if (!IsServer) return;
        currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
    }
    public void UpdateMaxHeal(float amount)
    {
        if (!IsServer) return;
        maxHealth = amount;
    }
}