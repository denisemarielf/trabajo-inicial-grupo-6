using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("--------Cronometro--------")]
    public float matchDuration = 120f; // 2 minutos
    private float timeRemaining;
    private bool matchEnded = false;

    [Header("--------UI--------")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private int enemiesSpawnCount;
    void OnEnable()
    {
        TowerHealth.OnTowerDestroyed += HandleTowerDestroyed;
    }
    void OnDisable()
    {
        TowerHealth.OnTowerDestroyed -= HandleTowerDestroyed;
    }
    void Start()
    {
        setCountEnemies(enemiesSpawnCount);
        timeRemaining = matchDuration;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        UpdateTimerUI();

    }

    // Update is called once per frame
    void Update()
    {
        if (matchEnded) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            Win();
        }

        UpdateTimerUI();

    }
    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
    private void HandleTowerDestroyed()
    {
        if (matchEnded) return;
        Lose();
    }
    private void Win()
    {
        matchEnded = true;

        if (winPanel != null) winPanel.SetActive(true);

        
    }
    private void Lose()
    {
        matchEnded = true;
        Debug.Log("DERROTA. La torre fue destruida.");

        if (losePanel != null) losePanel.SetActive(true);

        
    }

    private void setCountEnemies(int count)
    {
        GameObject[] spawnerObjs = GameObject.FindGameObjectsWithTag("Spawner");

        foreach (GameObject obj in spawnerObjs)
        {
            EnemySpawner spawner = obj.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                spawner.setEnemiesToSpawn(count);
            }
        }
    }
}
