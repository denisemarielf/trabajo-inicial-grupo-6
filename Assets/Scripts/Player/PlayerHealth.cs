using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : NetworkBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 100f;

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

    // Se dispara SOLO del lado del server cuando este jugador muere.
    // Ahora pasa el clientId para que GameManager sepa CUAL jugador murió
    // y pueda llevar la cuenta de cuantos quedan vivos.
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

    // Llamar solo desde código que YA corre en el server (ej. EnemyCombat.DealDamage,
    // que ya está gateado con IsServer). Si en el futuro el daño viniera de un cliente,
    // ese llamador necesita un [ServerRpc] propio, nunca invocar esto directo desde un cliente.
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        if (isDeadNet.Value) return;

        currentHealth.Value -= amount;
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

        // Avisa al GameManager (server-side) para el conteo global de jugadores vivos
        OnPlayerDied?.Invoke(OwnerClientId);

        // Muestra el panel de derrota SOLO en la pantalla del jugador que murió
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
        };
        ShowPersonalLoseClientRpc(clientRpcParams);
    }

    [ClientRpc]
    private void ShowPersonalLoseClientRpc(ClientRpcParams rpcParams = default)
    {
        // Este bloque corre en TODOS los clientes, pero gracias a ClientRpcParams
        // Netcode solo lo EJECUTA en el cliente cuyo OwnerClientId coincide.
        if (PersonalLosePanel.Instance != null)
        {
            PersonalLosePanel.Instance.Show();
        }
        else
        {
            Debug.LogWarning("[PlayerHealth] No se encontro PersonalLosePanel.Instance en la escena.");
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

    public bool IsDead() => isDeadNet.Value;
    public float GetHealthPercent() => currentHealth.Value / maxHealth;
}