using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 200f;
    private float currentHealth;

    private bool isDestroyed = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            DestroyTower();
        }
    }

    private void DestroyTower()
    {
        isDestroyed = true;

        Debug.Log("La torre fue destruida");
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
}
