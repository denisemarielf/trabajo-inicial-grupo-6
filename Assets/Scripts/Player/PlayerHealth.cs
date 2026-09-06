using System;
using Unity.Netcode;
using UnityEngine;

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
    // GameManager se suscribe igual que ya hace con TowerHealth.OnTowerDestroyed.
    public static event Action OnPlayerDied;

    private PlayerHealthUI healthUI;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
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

        OnPlayerDied?.Invoke();
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