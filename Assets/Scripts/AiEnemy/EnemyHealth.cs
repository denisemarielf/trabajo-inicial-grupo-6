using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("--------Referencias--------")]
    private Animator animator;
    public GameObject deathEffect; 
    public float destroyDelay = 3f;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponentInChildren<Animator>();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        if (animator != null)
            animator.SetTrigger("hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetTrigger("die");
        var ai = GetComponent<Ai>();
        if (ai != null) ai.enabled = false;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, destroyDelay);
    }

    public bool IsDead()
    {
        return isDead;
    }
}
