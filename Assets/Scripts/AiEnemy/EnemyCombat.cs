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
    private Transform player;
    private EnemyHealth enemyHealth;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        player = FindAnyObjectByType<PlayerMovementCC>().transform;
        enemyHealth = GetComponent<EnemyHealth>();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (enemyHealth != null && enemyHealth.IsDead()) return;
        if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh) return;


        if (distanceToPlayer <= attackRange)
        {
           
            navMeshAgent.isStopped = true;

            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
            }
        }
        else
        {
            navMeshAgent.isStopped = false;
        }


        
    }
   

        void Attack()
    {
        lastAttackTime = Time.time;

        if (animator != null)
            animator.SetTrigger("attack");

    }

    // Este metodo lo llama un Animation Event en el frame exacto
    // donde el arma/garra "conecta" en la animacion de ataque.
    /*public void DealDamage()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
        }
    }*/


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}