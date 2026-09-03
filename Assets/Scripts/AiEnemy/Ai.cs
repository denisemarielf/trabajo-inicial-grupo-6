using UnityEngine;
using UnityEngine.AI;

public class Ai : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
    public GameObject destination1;

    [Header("--------Follow Header--------")]
    private Transform player; // el jugador vivo más cercano en este momento (puede ser null)
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
    private float playerSearchInterval = 0.5f; // cada cuánto rebusca al jugador más cercano
    private float playerSearchTimer = 0f;


    public Transform CurrentPlayer => player;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        enemyCombat = GetComponent<EnemyCombat>();
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
        float speed = navMeshAgent.velocity.magnitude;
        animator.SetFloat("speed", speed);

    
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
        PlayerMovementCC[] allPlayers = FindObjectsByType<PlayerMovementCC>(FindObjectsSortMode.None);

        Transform nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (var p in allPlayers)
        {
            if (p == null) continue; 

            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = p.transform;
            }
        }

        player = nearest;
    }

    public void FollowPlayer()
    {
        if (player == null) return;

        if (Vector3.Distance(player.position, lastPlayerPosition) > repathThreshold)
        {
            navMeshAgent.destination = player.position;
            lastPlayerPosition = player.position;
        }
    }

    public void GoToDestination()
    {
        if (destination1 == null) return;
        navMeshAgent.destination = destination1.transform.position;
    }
}