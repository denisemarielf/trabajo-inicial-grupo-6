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
    [Tooltip("Si es true, vuelve automaticamente al menu tras el retraso configurado. Si es false, los jugadores usan los botones de la pantalla de fin de partida.")]
    [SerializeField] private bool autoReturnToMenu = false;
    [SerializeField] private float returnToMenuDelay = 3f;
    [SerializeField] private string menuSceneName = "menuPrincipal";

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
    private NetworkVariable<int> totalKills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    private NetworkVariable<int> matchEndReason = new NetworkVariable<int>(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsMatchOver => networkMatchState != null && networkMatchState.Value != 0;
    public int TotalKills => totalKills != null ? totalKills.Value : 0;

    [Header("--------UI--------")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private int enemiesToSpawn;
    [SerializeField] private float waveInterval = 30f;

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
            CleanUpMigratedEnemies();
            ConfigureSpawners(enemiesToSpawn, waveInterval);
            networkTimeRemaining.Value = matchDuration;
            TowerHealth.OnTowerDestroyed += HandleTowerDestroyed;
            PlayerHealth.OnPlayerDied += HandlePlayerDied;
            EnemyHealth.OnEnemyDied += HandleEnemyDied;

            StartCoroutine(ResetPlayersRoutine());
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
            EnemyHealth.OnEnemyDied -= HandleEnemyDied;
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
            matchEndReason.Value = (int)MatchResultReason.SurviveTime;
            Win();
        }
    }

    public void RegisterPlayer(ulong clientId)
    {
        if (!IsServer) return;
        alivePlayers.Add(clientId);
        Debug.Log($"[GameManager] RegisterPlayer({clientId}). Total registrados: {alivePlayers.Count} -> [{string.Join(",", alivePlayers)}]");
    }

    private void CleanUpMigratedEnemies()
    {
        if (!IsServer) return;
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include);
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (enemy.NetworkObject != null && enemy.NetworkObject.IsSpawned)
            {
                enemy.NetworkObject.Despawn(true);
            }
            else
            {
                Destroy(enemy.gameObject);
            }
        }
    }

    private IEnumerator ResetPlayersRoutine()
    {
        // Esperar un frame a que la escena se asiente
        yield return null;

        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Include);
        Vector3 baseSpawn = new Vector3(270f, 33f, 250f);
        int i = 0;
        alivePlayers.Clear();

        foreach (var ph in allPlayers)
        {
            if (ph == null) continue;
            Vector3 spawnPos = baseSpawn + new Vector3(i * 2.5f, 0f, 0f);
            ph.ResetPlayerForNewMatch(spawnPos);
            alivePlayers.Add(ph.OwnerClientId);
            i++;
        }
        Debug.Log($"[GameManager] Jugadores reiniciados: {alivePlayers.Count} registrados.");

        // Los jugadores (y sus armas) persisten entre reinicios, pero la pantalla
        // de fin de partida anterior dejó Shoot/PlayerMovementCC deshabilitados
        // en TODOS los clientes. Avisamos para que cada uno reactive sus controles.
        ReactivarControlesClientRpc();
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
        matchEndReason.Value = (int)MatchResultReason.TowerDestroyed;
        Lose();
    }

    private void HandlePlayerDied(ulong clientId)
    {
        if (networkMatchState.Value != 0) return;

        alivePlayers.Remove(clientId);
        Debug.Log($"[GameManager] Jugador {clientId} murio. Quedan vivos: {alivePlayers.Count} -> [{string.Join(",", alivePlayers)}]");

        if (alivePlayers.Count <= 0)
        {
            Debug.Log("DERROTA GLOBAL. Todos los jugadores murieron.");
            matchEndReason.Value = (int)MatchResultReason.OutOfLives;
            Lose();
        }
    }

    private void HandleEnemyDied()
    {
        if (!IsServer) return;
        totalKills.Value++;
        Debug.Log($"[GameManager] Enemigo eliminado. Total bajas: {totalKills.Value}");
    }

    private void Win()
    {
        if (networkMatchState.Value != 0) return;
        MatchResultReason reason = (matchEndReason.Value >= 0)
            ? (MatchResultReason)matchEndReason.Value
            : MatchResultReason.SurviveTime;
        matchEndReason.Value = (int)reason;
        networkMatchState.Value = 1;

        ShowGameOverClientRpc((int)reason, totalKills.Value);

        if (autoReturnToMenu) StartCoroutine(EndMatchRoutine());
    }

    private void Lose()
    {
        if (networkMatchState.Value != 0) return;
        MatchResultReason reason = (matchEndReason.Value >= 0)
            ? (MatchResultReason)matchEndReason.Value
            : MatchResultReason.OutOfLives;
        matchEndReason.Value = (int)reason;
        networkMatchState.Value = 2;

        ShowGameOverClientRpc((int)reason, totalKills.Value);

        if (autoReturnToMenu) StartCoroutine(EndMatchRoutine());
    }

    [ClientRpc]
    private void ShowGameOverClientRpc(int reasonInt, int kills)
    {
        Debug.Log($"[GameManager] ShowGameOverClientRpc recibido. Motivo: {(MatchResultReason)reasonInt}, Bajas: {kills}");
        GameOverUI ui = GameOverUI.EnsureInstance();
        if (ui != null)
        {
            ui.Show((MatchResultReason)reasonInt, kills);
        }
    }

    // Se llama una vez reseteados los jugadores tras un reinicio de partida.
    // GameOverUI.Hide() ya hace exactamente lo que necesitamos: oculta el panel
    // (por si quedó activo) y reactiva Shoot/PlayerMovementCC/cámara/input de
    // TODOS los jugadores en ESTE cliente.
    [ClientRpc]
    private void ReactivarControlesClientRpc()
    {
        GameOverUI ui = GameOverUI.EnsureInstance();
        if (ui != null)
        {
            ui.Hide();
        }
    }

    private IEnumerator EndMatchRoutine()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        NetworkManager.Singleton.SceneManager.LoadScene(menuSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    private void OnMatchStateChanged(int oldState, int newState)
    {
        if (newState == 1 && winPanel != null) winPanel.SetActive(true);
        if (newState == 2 && losePanel != null) losePanel.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartMatchServerRpc()
    {
        Debug.Log("[GameManager] Servidor recibio peticion de reinicio de partida.");
        RestartMatch();
    }

    public void RestartMatch()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsServer)
        {
            CleanUpMigratedEnemies();

            if (NetworkManager.Singleton.SceneManager != null)
            {
                Debug.Log("[GameManager] Reiniciando partida sincronizada con NetworkSceneManager...");
                NetworkManager.Singleton.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    UnityEngine.SceneManagement.LoadSceneMode.Single
                );
                return;
            }
        }

        Debug.Log("[GameManager] Reiniciando escena localmente...");
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    public void ConfigureSpawners(int count, float interval)
    {
        GameObject[] spawnerObjs = GameObject.FindGameObjectsWithTag("Spawner");
        foreach (GameObject obj in spawnerObjs)
        {
            EnemySpawner spawner = obj.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                spawner.setEnemiesToSpawn(count);
                spawner.setWaveInterval(interval);
            }
        }
    }
}