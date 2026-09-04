using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [Header("--------Cronometro--------")]
    public float matchDuration = 120f; // 2 minutos

    private NetworkVariable<float> networkTimeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<int> networkMatchState = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("--------UI--------")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private int enemiesSpawnCount;

    public override void OnNetworkSpawn()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (IsServer)
        {
            networkTimeRemaining.Value = matchDuration;
            TowerHealth.OnTowerDestroyed += HandleTowerDestroyed;
        }

        
        networkMatchState.OnValueChanged += OnMatchStateChanged;
        networkTimeRemaining.OnValueChanged += (oldVal, newVal) => UpdateTimerUI(newVal);

        UpdateTimerUI(networkTimeRemaining.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            TowerHealth.OnTowerDestroyed -= HandleTowerDestroyed;
        }
        networkMatchState.OnValueChanged -= OnMatchStateChanged;
    }

    void Update()
    {
       
        if (!IsServer) return;
        if (networkMatchState.Value != 0) return;

        networkTimeRemaining.Value -= Time.deltaTime;

        if (networkTimeRemaining.Value <= 0)
        {
            networkTimeRemaining.Value = 0;
            Win();
        }
    }

    private void UpdateTimerUI(float time)
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void HandleTowerDestroyed()
    {
       
        if (networkMatchState.Value != 0) return;
        Lose();
    }

    private void Win()
    {
        networkMatchState.Value = 1;
    }

    private void Lose()
    {
        Debug.Log("DERROTA. La torre fue destruida.");
        networkMatchState.Value = 2;
    }

   
    private void OnMatchStateChanged(int oldState, int newState)
    {
        if (newState == 1 && winPanel != null) winPanel.SetActive(true);
        if (newState == 2 && losePanel != null) losePanel.SetActive(true);
    }


    /*
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
    }*/

}
