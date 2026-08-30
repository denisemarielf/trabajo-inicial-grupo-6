using TMPro;
using UnityEngine;

public class TowerUi : MonoBehaviour
{
    [SerializeField] private TMP_Text towerCurrentHealth;
    [SerializeField] private TowerHealth towerHealth;

    void Start()
    {
        if (towerHealth == null)
            towerHealth = FindAnyObjectByType<TowerHealth>();
    }

    void Update()
    {
        updateHealthTower();
    }



    public void updateHealthTower()
    {
        if (towerHealth == null)
        {
            towerCurrentHealth.text = " ";
            return;
        }

        towerCurrentHealth.text = towerHealth.GetCurrentHealth().ToString();
       

    }
}
