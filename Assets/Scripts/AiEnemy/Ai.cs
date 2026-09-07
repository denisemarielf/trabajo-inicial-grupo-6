using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class Ai : NetworkBehaviour
{
    public NavMeshAgent navMeshAgent;
    public GameObject destination1;

    [Header("--------Follow Header--------")]
    private Transform player; // el jugador vivo mas cercano en este momento (puede ser null)
    private PlayerHealth playerHealth;
    public bool followPlayer;
    private float distanceToPlayer;
    [SerializeField] private float distanceToFollowPlayer = 30f;  // para EMPEZAR a seguir
    [SerializeField] private float distanceToLosePlayer = 40f;    // para DEJAR de seguir (mas grande = mas persistente)
    private Animator animator;

    [Header("--------Combat Header--------")]
    private EnemyCombat enemyCombat;
    private Vector3 lastPlayerPosition;
    private float repathThreshold = 0.5f;
    private bool isFollowingPlayer;

    [Header("--------Player Search--------")]
    private float playerSearchInterval = 0.25f; // cada cuanto rebusca al jugador mas cercano
    private float playerSearchTimer = 0f;

    [Header("--------Aggro (dano recibido)--------")]
    [SerializeField] private float aggroDuration = 6f; // cuanto tiempo prioriza a quien le pego
    private Transform aggroTarget;
    private PlayerHealth aggroTargetHealth;
    private float aggroTimer = 0f;

    public Transform CurrentPlayer => player;

    // Sincroniza la velocidad para que la animacion se vea bien en TODOS los clientes,
    // no solo en el server (que es el unico que realmente mueve el NavMeshAgent).
    private NetworkVariable<float> networkSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyCombat = GetComponent<EnemyCombat>();

        // La IA solo la calcula el server. Los clientes solo reciben el resultado
        // via NetworkTransform (posicion) y networkSpeed (animacion).
        if (!IsServer) return;

        RefreshNearestPlayer();
        SetInitialDestination();
    }

    private void SetInitialDestination()
    {
        if (destination1 == null) return;
        if (!navMeshAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                navMeshAgent.Warp(hit.position);
            }
            else
            {
                return;
            }
        }
        navMeshAgent.destination = destination1.transform.position;
    }

    void Update()
    {
        // Si la partida termino, detener animacion y navegacion
        if (GameManager.Instance != null && GameManager.Instance.IsMatchOver)
        {
            if (IsServer)
            {
                if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
                {
                    navMeshAgent.isStopped = true;
                    navMeshAgent.velocity = Vector3.zero;
                }
                networkSpeed.Value = 0f;
            }

            if (animator != null)
            {
                animator.SetFloat("speed", 0f);
            }
            return;
        }

        // La animacion se actualiza en TODOS los clientes, leyendo el valor sincronizado.
        if (animator != null)
        {
            animator.SetFloat("speed", networkSpeed.Value);
        }

        // Toda la logica de decision (pathfinding, busqueda de jugador, etc.)
        // corre unicamente en el server.
        if (!IsServer) return;

        // Cuenta regresiva del aggro por dano
        if (aggroTimer > 0f)
        {
            aggroTimer -= Time.deltaTime;

            // Si el jugador que genero el aggro murio, se cancela el aggro
            if (aggroTargetHealth != null && aggroTargetHealth.IsDead())
            {
                ClearAggro();
            }
        }
        else if (aggroTarget != null)
        {
            ClearAggro();
        }

        if (playerHealth != null && playerHealth.IsDead())
        {
            player = null;
            playerHealth = null;
        }

        float speed = (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh) ? navMeshAgent.velocity.magnitude : 0f;
        networkSpeed.Value = speed;

        // Mientras haya aggro activo, NO se rebusca al jugador mas cercano:
        // el enemigo se queda enfocado en quien le pego.
        if (aggroTimer <= 0f)
        {
            playerSearchTimer -= Time.deltaTime;
            if (playerSearchTimer <= 0f)
            {
                RefreshNearestPlayer();
                playerSearchTimer = playerSearchInterval;
            }
        }

        // El target efectivo es el de aggro si esta activo, sino el mas cercano normal
        Transform effectiveTarget = (aggroTimer > 0f && aggroTarget != null) ? aggroTarget : player;

        if (effectiveTarget == null)
        {
            if (isFollowingPlayer)
            {
                GoToDestination();
                isFollowingPlayer = false;
            }
            return;
        }

        distanceToPlayer = Vector3.Distance(transform.position, effectiveTarget.position);
        bool isInAttackRange = enemyCombat != null && distanceToPlayer <= enemyCombat.attackRange;

        // Con aggro activo, el rango de seguimiento no importa: persigue igual.
        bool withinFollowRange = isFollowingPlayer
            ? distanceToPlayer < distanceToLosePlayer
            : distanceToPlayer < distanceToFollowPlayer;

        bool shouldFollowPlayer = (aggroTimer > 0f || withinFollowRange) && followPlayer && !isInAttackRange;

        if (shouldFollowPlayer)
        {
            FollowTarget(effectiveTarget);
            isFollowingPlayer = true;
        }
        else if (isFollowingPlayer)
        {
            GoToDestination();
            isFollowingPlayer = false;
        }
    }

    // Llamado desde EnemyHealth.TakeDamage cuando un jugador le pega a este enemigo.
    public void SetAggroTarget(Transform attacker)
    {
        if (attacker == null) return;

        aggroTarget = attacker;
        aggroTargetHealth = attacker.GetComponent<PlayerHealth>();
        aggroTimer = aggroDuration;
    }

    private void ClearAggro()
    {
        aggroTarget = null;
        aggroTargetHealth = null;
        aggroTimer = 0f;
    }

    private void RefreshNearestPlayer()
    {
        PlayerMovementCC[] allPlayers = FindObjectsByType<PlayerMovementCC>(
            FindObjectsSortMode.None
        );
        Transform nearest = null;
        PlayerHealth nearestHealth = null;
        float nearestDist = Mathf.Infinity;
        foreach (var p in allPlayers)
        {
            if (p == null) continue;
            PlayerHealth health = p.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead()) continue;
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = p.transform;
                nearestHealth = health;
            }
        }
        player = nearest;
        playerHealth = nearestHealth;
    }

    public void FollowTarget(Transform target)
    {
        if (target == null) return;
        if (Vector3.Distance(target.position, lastPlayerPosition) > repathThreshold)
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.destination = target.position;
            }
            lastPlayerPosition = target.position;
        }
    }

    // Mantenido por compatibilidad si algo mas lo llamaba directo
    public void FollowPlayer()
    {
        FollowTarget(player);
    }

    public void GoToDestination()
    {
        if (destination1 == null) return;
        if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.destination = destination1.transform.position;
        }
    }
}