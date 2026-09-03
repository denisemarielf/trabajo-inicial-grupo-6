using UnityEngine;
using Unity.Netcode;

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
    public GameObject deathEffect;
    public float destroyDelay = 3f;

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
    }

    // Llamar solo desde código que ya corre en el server (por ejemplo
    // EnemyCombat.DealDamage, que ya está gateado con IsServer).
    // Si en el futuro el daño se origina en un cliente (ej. arma de jugador),
    // ese script necesita un [ServerRpc] que llame a esto, nunca llamarlo directo desde un cliente.
    public void TakeDamage(float amount)
    {
        if (!IsServer) return;
        if (isDeadNet.Value) return;

        currentHealth.Value -= amount;

        if (animator != null)
            animator.SetTrigger("hit"); // NetworkAnimator lo replica a los clientes

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!IsServer) return;
        isDeadNet.Value = true;

        if (animator != null)
            animator.SetTrigger("die");

        var ai = GetComponent<Ai>();
        if (ai != null) ai.enabled = false;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (deathEffect != null)
        {
            // Si deathEffect necesita verse en todos los clientes, spawnealo como
            // NetworkObject en vez de Instantiate local, o disparalo vía ClientRpc.
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Despawnea en la red (destruye en todos los clientes). true = también destruye el GameObject.
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