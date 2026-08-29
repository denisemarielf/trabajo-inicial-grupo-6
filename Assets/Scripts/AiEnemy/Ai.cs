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
    private float distanceToFollowPlayer = 10;
    private Animator animator;


    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        navMeshAgent.destination = destination1.transform.position;
        player = FindAnyObjectByType<PlayerMovementCC>().gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        float speed = navMeshAgent.velocity.magnitude;
        animator.SetFloat("speed", speed);

        distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if(distanceToPlayer < distanceToFollowPlayer && followPlayer)
        {
            FollowPlayer();
        }
        
    }
    
    
    public void FollowPlayer()
    {
        navMeshAgent.destination = player.transform.position;

    }
}
