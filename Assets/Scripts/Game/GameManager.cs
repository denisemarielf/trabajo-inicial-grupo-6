using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--------Cronometro--------")]
    public float matchDuration = 120f; // 2 minutos

    [Header("--------Config--------")]
    [SerializeField] private float returnToMenuDelay = 3f;
    [SerializeField] private string menuSceneName = "MainMenu";

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

    // Server-only: jugadores que siguen vivos en la partida
    private readonly HashSet<ulong> alivePlayers = new HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (IsServer)
        {
            networkTimeRemaining.Value = matchDuration;
            TowerHealth.OnTowerDestroyed += HandleTowerDestroyed;
            PlayerHealth.OnPlayerDied += HandlePlayerDied;
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
            PlayerHealth.OnPlayerDied -= HandlePlayerDied;
        }
        networkMatchState.OnValueChanged -= OnMatchStateChanged;
    }

    void Update()
    {
        // Solo el servidor descuenta el tiempo
        if (!IsServer) return;
        if (networkMatchState.Value != 0) return;

        networkTimeRemaining.Value -= Time.deltaTime;
        if (networkTimeRemaining.Value <= 0)
        {
            networkTimeRemaining.Value = 0;
            Win();
        }
    }

    // Llamado por PlayerHealth (server-side) cuando un jugador spawnea, para saber cuantos hay en total
    public void RegisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        alivePlayers.Add(clientId);
        Debug.Log($"[GameManager] RegisterPlayer({clientId}). Total registrados: {alivePlayers.Count} -> [{string.Join(",", alivePlayers)}]");
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
        Debug.Log("DERROTA GLOBAL. La torre fue destruida.");
        Lose();
    }

    // Ahora recibe el clientId del jugador que murio
    private void HandlePlayerDied(ulong clientId)
    {
        if (networkMatchState.Value != 0) return;

        alivePlayers.Remove(clientId);
        Debug.Log($"[GameManager] Jugador {clientId} murio. Quedan vivos: {alivePlayers.Count} -> [{string.Join(",", alivePlayers)}]");

        // Solo es derrota GLOBAL si ya no queda nadie vivo
        if (alivePlayers.Count <= 0)
        {
            Debug.Log("DERROTA GLOBAL. Todos los jugadores murieron.");
            Lose();
        }
        // Si quedan jugadores vivos, no pasa nada a nivel global.
        // El jugador que murio ya recibe su propio panel individual desde PlayerHealth.
    }

    private void Win()
    {
        if (networkMatchState.Value != 0) return;
        networkMatchState.Value = 1;
        StartCoroutine(EndMatchRoutine());
    }

    private void Lose()
    {
        if (networkMatchState.Value != 0) return;
        networkMatchState.Value = 2;
        StartCoroutine(EndMatchRoutine());
    }

    // Server-only: espera y luego manda a TODOS los clientes conectados al menu
    private IEnumerator EndMatchRoutine()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        NetworkManager.Singleton.SceneManager.LoadScene("menuPrincipal", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnMatchStateChanged(int oldState, int newState)
    {
        if (newState == 1 && winPanel != null) winPanel.SetActive(true);
        if (newState == 2 && losePanel != null) losePanel.SetActive(true);
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