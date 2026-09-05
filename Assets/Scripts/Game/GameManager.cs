using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("--------Cronometro--------")]
    public float matchDuration = 120f;
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
        PlayerHealth.OnPlayerDied += HandlePlayerDied;
    }
    void OnDisable()
    {
        TowerHealth.OnTowerDestroyed -= HandleTowerDestroyed;
        PlayerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    void Start()
    {
        timeRemaining = matchDuration;
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        UpdateTimerUI();
    }

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
        LoseClientRpc();
    }

    private void HandlePlayerDied()
    {
        if (matchEnded) return;
        LoseClientRpc();
    }

    [ClientRpc]
    private void LoseClientRpc()
    {
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
        Debug.Log("DERROTA.");
        if (losePanel != null) losePanel.SetActive(true);
    }
}