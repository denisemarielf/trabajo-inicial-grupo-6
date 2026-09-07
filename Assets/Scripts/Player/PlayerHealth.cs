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
    [Tooltip("Si es true, el cliente se desconecta automáticamente al morir tras el retraso. Si es false, espera en la partida a que termine para ver la pantalla de fin de partida.")]
    [SerializeField] private bool disconnectClientOnDeath = false;
    [SerializeField] private float returnToMenuDelay = 3f;
    [SerializeField] private string menuSceneName = "menuPrincipal";

    private Coroutine returnRoutine;

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
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver) return;

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

        // Si la partida ya terminó a nivel global (derrota o victoria global),
        // no enviamos derrota individual con desconexión: se muestra GameOverUI sincronizado.
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
        {
            return;
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        ShowPersonalLoseClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void ShowPersonalLoseClientRpc(ClientRpcParams rpcParams = default)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
        {
            return;
        }

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

        if (disconnectClientOnDeath)
        {
            if (returnRoutine != null) StopCoroutine(returnRoutine);
            returnRoutine = StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    private IEnumerator ReturnToMenuAfterDelay()
    {
        yield return new WaitForSeconds(returnToMenuDelay);

        // Si la partida terminó a nivel global mientras esperaba, NO desconectar
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
        {
            returnRoutine = null;
            yield break;
        }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown(); // se desconecta de la partida

        SceneManager.LoadScene(menuSceneName); // vuelve a SU menú local
        returnRoutine = null;
    }

    public void CancelReturnToMenu()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
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