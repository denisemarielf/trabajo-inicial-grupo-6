using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
 private EnemyCombat enemyCombat;

void Start()
{
    enemyCombat = GetComponentInParent<EnemyCombat>();

    if (enemyCombat == null)
        Debug.LogWarning($"{name}: no se encontró EnemyCombat en ningún padre.");
}

public void DealDamage()
{
    if (enemyCombat != null)
        enemyCombat.DealDamage();
}
}
 
