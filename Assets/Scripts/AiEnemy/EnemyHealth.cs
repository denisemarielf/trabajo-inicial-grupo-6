using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // para NetworkAnimator

public class EnemyHealth : NetworkBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 100f;

    // Sincronizada: todos los clientes ven la misma vida, pero solo el server la escribe.
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("--------Referencias--------")]
    private Animator animator;
    private NetworkAnimator networkAnimator;
    private Ai ai;
    public GameObject deathEffect;
    public float destroyDelay = 3f;

    [Header("--------Audio--------")]
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    public static event Action OnEnemyDied;

    private NetworkVariable<bool> isDeadNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
        if (networkAnimator == null)
            networkAnimator = GetComponent<NetworkAnimator>();
        ai = GetComponent<Ai>();
    }

    // "attacker" es opcional: si quien llama no lo pasa, el enemigo se dana igual
    // pero no se genera aggro (compatibilidad con llamados viejos sin romper nada).
    public void TakeDamage(float amount, Transform attacker = null)
    {
        if (!IsServer) return;
        if (isDeadNet.Value) return;

        currentHealth.Value -= amount;

        // Marca a quien pego como blanco prioritario por un rato
        if (attacker != null && ai != null)
        {
            ai.SetAggroTarget(attacker);
        }

        if (networkAnimator != null)
            networkAnimator.SetTrigger("hit");
        if (animator != null)
            animator.SetTrigger("hit");

        if (IsServer && audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;

        isDeadNet.Value = true;
        OnEnemyDied?.Invoke();

        if (networkAnimator != null)
            networkAnimator.SetTrigger("die");
        if (animator != null)
            animator.SetTrigger("die");

        if (IsServer && audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        var aiComp = GetComponent<Ai>();
        if (aiComp != null) aiComp.enabled = false;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        Invoke(nameof(DespawnSelf), destroyDelay);
    }

    private void DespawnSelf()
    {
        if (!IsServer) return;
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsDead()
    {
        return isDeadNet.Value;
    }
}