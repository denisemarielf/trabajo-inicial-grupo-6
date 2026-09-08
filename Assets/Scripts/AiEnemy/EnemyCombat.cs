using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using Unity.Netcode.Components; // para NetworkAnimator

public class EnemyCombat : NetworkBehaviour
{
    [Header("--------Ataque--------")]
    public float attackDamage = 15f;
    public float attackRange = 2.5f;
    public float attackCooldown = 0.8f;
    private float lastAttackTime;
    [Header("--------Referencias--------")]
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private Ai ai; // fuente del jugador actual (vivo mas cercano)
    private EnemyHealth enemyHealth;
    private Transform currentTarget;
    private Transform tower;
    private Collider towerCollider;
    private NetworkAnimator networkAnimator;
    public AudioSource audioSource;
    public AudioClip attackSound;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        networkAnimator = GetComponentInChildren<NetworkAnimator>();
        if (networkAnimator == null)
            networkAnimator = GetComponent<NetworkAnimator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        ai = GetComponent<Ai>();
        enemyHealth = GetComponent<EnemyHealth>();
        GameObject towerObj = GameObject.FindGameObjectWithTag("Tower");
        if (towerObj != null)
        {
            tower = towerObj.transform;
            towerCollider = towerObj.GetComponent<Collider>();
        }
    }

    void Update()
    {
        // Toda la logica de combate (decision de a quien atacar, cooldown, dano)
        // la calcula unicamente el server. El NetworkAnimator se encarga de que
        // los clientes vean la animacion igual.
        if (!IsServer) return;

        // Si la partida termino, no atacar ni moverse mas
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
            }
            return;
        }

        if (enemyHealth != null && enemyHealth.IsDead()) return;
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        // El jugador actual lo decide Ai.cs (el vivo mas cercano, o null si no hay ninguno).
        Transform player = ai != null ? ai.CurrentPlayer : null;
        float distanceToPlayer = player != null
            ? Vector3.Distance(transform.position, player.position)
            : Mathf.Infinity;

        float distanceToTower = Mathf.Infinity;
        if (towerCollider != null)
        {
            Vector3 closestPoint = towerCollider.ClosestPoint(transform.position);
            distanceToTower = Vector3.Distance(transform.position, closestPoint);
        }

        bool playerInRange = distanceToPlayer <= attackRange;
        bool towerInRange = distanceToTower <= attackRange;

        if (playerInRange || towerInRange)
        {
            currentTarget = (distanceToPlayer <= distanceToTower) ? player : tower;
            navMeshAgent.isStopped = true;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            currentTarget = null;
            navMeshAgent.isStopped = false;
        }
    }

    void Attack()
    {
        lastAttackTime = Time.time;
        if (networkAnimator != null)
            networkAnimator.SetTrigger("attack");
    }

    // Llamado desde un Animation Event dentro del clip de ataque.
    public void DealDamage()
    {
        // Con NetworkAnimator, el Animation Event dispara en TODOS los clientes
        // (porque la animacion esta sincronizada). Sin este check, cada cliente
        // aplicaria dano por su cuenta.
        if (!IsServer) return;

        // Si la partida ya termino a nivel global, no infligir dano
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver) return;

        if (currentTarget == null) return;

        if (currentTarget.CompareTag("Tower"))
        {
            TowerHealth towerHealth = currentTarget.GetComponent<TowerHealth>();
            if (towerHealth != null)
                towerHealth.TakeDamage(attackDamage);
        }
        else if (currentTarget.CompareTag("Player"))
        {
            PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                PlayAttackSoundClientRpc();
            }
        }
    }

    [ClientRpc]
    private void PlayAttackSoundClientRpc()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
