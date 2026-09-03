using UnityEngine;
using UnityEngine.AI;

public class EnemyCombat : MonoBehaviour
{
    [Header("--------Ataque--------")]
    public float attackDamage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    private float lastAttackTime;

    [Header("--------Referencias--------")]
    private Animator animator;
    private NavMeshAgent navMeshAgent;
    private Ai ai; // fuente del jugador actual (vivo más cercano)
    private EnemyHealth enemyHealth;
    private Transform currentTarget;
    private Transform tower;
    private Collider towerCollider;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
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
        if (enemyHealth != null && enemyHealth.IsDead()) return;
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;

        // El jugador actual lo decide Ai.cs (el vivo más cercano, o null si no hay ninguno).
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
        if (animator != null)
            animator.SetTrigger("attack");
    }

    public void DealDamage()
    {
        if (currentTarget == null) return;

        if (currentTarget.CompareTag("Tower"))
        {
            TowerHealth towerHealth = currentTarget.GetComponent<TowerHealth>();
            if (towerHealth != null)
                towerHealth.TakeDamage(attackDamage);
        }
        else if (currentTarget.CompareTag("Player"))
        {
           /* PlayerHealth playerHealth = currentTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(attackDamage);*/
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}