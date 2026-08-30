using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("--------Spawn--------")]
    public GameObject enemyPrefab;      
    public Transform spawnPoint;        
    public int enemiesToSpawn = 20;
    public float timeBetweenSpawns = 2f;

    void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }
}