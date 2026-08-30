using UnityEngine;
using UnityEngine.AI;

public class Ai : MonoBehaviour
{
    public NavMeshAgent navMeshAgent;
    public GameObject destination1;

    [Header("--------Follow Header--------")]
    private GameObject player;
    public bool followPlayer;
    private float distanceToPlayer;
    private float distanceToFollowPlayer = 20;
    private Animator animator;

    [Header("--------Combat Header--------")]
    private EnemyCombat enemyCombat;
    private Vector3 lastPlayerPosition;
    private float repathThreshold = 0.5f; 
    private bool isFollowingPlayer; 

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        navMeshAgent.destination = destination1.transform.position;
        player = FindAnyObjectByType<PlayerMovementCC>().gameObject;
        enemyCombat = GetComponent<EnemyCombat>();
    }

    void Update()
    {
        float speed = navMeshAgent.velocity.magnitude;
        animator.SetFloat("speed", speed);

        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
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

    public void FollowPlayer()
    {
        if (Vector3.Distance(player.transform.position, lastPlayerPosition) > repathThreshold)
        {
            navMeshAgent.destination = player.transform.position;
            lastPlayerPosition = player.transform.position;
        }
    }

    public void GoToDestination()
    {
        navMeshAgent.destination = destination1.transform.position;
    }
}
