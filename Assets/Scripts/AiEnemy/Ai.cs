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
    private float distanceToFollowPlayer = 20;
    private Animator animator;
    [Header("--------Combat Header--------")]
    private EnemyCombat enemyCombat;
    private Vector3 lastPlayerPosition;
    private float repathThreshold = 0.5f;
    private bool isFollowingPlayer;
    [Header("--------Player Search--------")]
    private float playerSearchInterval = 0.5f; // cada cuanto rebusca al jugador mas cercano
    private float playerSearchTimer = 0f;
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

        if (playerHealth != null && playerHealth.IsDead())
        {
            player = null;
            playerHealth = null;
        }
        float speed = (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh) ? navMeshAgent.velocity.magnitude : 0f;
        networkSpeed.Value = speed;

        playerSearchTimer -= Time.deltaTime;
        if (playerSearchTimer <= 0f)
        {
            RefreshNearestPlayer();
            playerSearchTimer = playerSearchInterval;
        }
        if (player == null)
        {
            if (isFollowingPlayer)
            {
                GoToDestination();
                isFollowingPlayer = false;
            }
            return;
        }
        distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool isInAttackRange = enemyCombat != null && distanceToPlayer <= enemyCombat.attackRange;
        bool shouldFollowPlayer = distanceToPlayer < distanceToFollowPlayer && followPlayer && !isInAttackRange;
        if (shouldFollowPlayer)
        {
            FollowPlayer();
            isFollowingPlayer = true;
        }
        else if (isFollowingPlayer)
        {
            GoToDestination();
            isFollowingPlayer = false;
        }
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

    public void FollowPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(player.position, lastPlayerPosition) > repathThreshold)
        {
            if (navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.destination = player.position;
            }
            lastPlayerPosition = player.position;
        }
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
