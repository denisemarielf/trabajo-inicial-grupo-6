using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    [Header("--------Vida--------")]
    public float maxHealth = 200f;
    private float currentHealth;
    public AudioClip destroyTowerSound;

    private bool isDestroyed = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isDestroyed) return;

        Debug.Log($"Torre recibió {amount} de daño. Vida actual: {currentHealth}/{maxHealth}");
        currentHealth -= amount;
        

        if (currentHealth <= 0)
        {
            DestroyTower();
        }
    }

    private void DestroyTower()
    {
        isDestroyed = true;

        AudioSource.PlayClipAtPoint(destroyTowerSound, transform.position);
        Destroy(gameObject);
        
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
