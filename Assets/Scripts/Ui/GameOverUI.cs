using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_NETCODE
using Unity.Netcode;
#endif

public enum MatchResultReason
{
    SurviveTime,       // Victoria: Sobrevivieron el tiempo límite
    DefeatedAllEnemies,// Victoria: Mataron a todos los enemigos
    OutOfLives,        // Derrota: Se quedaron sin vidas
    TowerDestroyed     // Derrota: La torre fue destruida
}

public class GameOverUI : MonoBehaviour
{
    [Header("Paneles Principales")]
    [Tooltip("El panel contenedor general de la pantalla de fin de partida")]
    [SerializeField] private GameObject rootPanel;

    [Header("Textos de Resultado")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Estilos Visuales")]
    [SerializeField] private string victoryTitle = "¡VICTORIA!";
    [SerializeField] private Color victoryColor = new Color(0.2f, 0.85f, 0.35f, 1f); // Verde éxito / neón

    [SerializeField] private string defeatTitle = "DERROTA";
    [SerializeField] private Color defeatColor = new Color(0.9f, 0.22f, 0.22f, 1f); // Rojo alerta

    [Header("Mensajes de Victoria")]
    [SerializeField] private string victorySurviveSubtitle = "¡Han sobrevivido el tiempo límite!";
    [SerializeField] private string victoryAllEnemiesSubtitle = "¡Han eliminado a todos los enemigos!";

    [Header("Mensajes de Derrota")]
    [SerializeField] private string defeatOutOfLivesSubtitle = "Todos los jugadores se quedaron sin vidas.";
    [SerializeField] private string defeatTowerDestroyedSubtitle = "¡La torre ha sido destruida!";

    [Header("Estadísticas Opcionales")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private TMP_Text killsValueText;
    [SerializeField] private TMP_Text deathsValueText;

    [Header("Botones")]
    [SerializeField] private Button btnPlayAgain;
    [SerializeField] private Button btnMainMenu;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del menú principal para volver")]
    [SerializeField] private string mainMenuSceneName = "menuPrincipal";

    private void Awake()
    {
        // Aseguramos que arranque oculto por defecto
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }

        // Listener para volver al menú
        if (btnMainMenu != null)
        {
            btnMainMenu.onClick.AddListener(OnMainMenuClicked);
        }

        // Listener para jugar de nuevo / revancha
        if (btnPlayAgain != null)
        {
            btnPlayAgain.onClick.AddListener(OnPlayAgainClicked);
        }
    }

    private void OnDestroy()
    {
        if (btnMainMenu != null)
        {
            btnMainMenu.onClick.RemoveListener(OnMainMenuClicked);
        }

        if (btnPlayAgain != null)
        {
            btnPlayAgain.onClick.RemoveListener(OnPlayAgainClicked);
        }
    }

    /// <summary>
    /// Muestra la pantalla indicando el motivo específico del resultado.
    /// </summary>
    public void Show(MatchResultReason reason)
    {
        bool isVictory = (reason == MatchResultReason.SurviveTime || reason == MatchResultReason.DefeatedAllEnemies);

        string reasonSubtitle = reason switch
        {
            MatchResultReason.SurviveTime => victorySurviveSubtitle,
            MatchResultReason.DefeatedAllEnemies => victoryAllEnemiesSubtitle,
            MatchResultReason.OutOfLives => defeatOutOfLivesSubtitle,
            MatchResultReason.TowerDestroyed => defeatTowerDestroyedSubtitle,
            _ => string.Empty
        };

        DisplayUI(isVictory, reasonSubtitle);
    }

    /// <summary>
    /// Sobrecarga con motivo específico y estadísticas.
    /// </summary>
    public void Show(MatchResultReason reason, int kills, int deaths)
    {
        Show(reason);
        SetStats(kills, deaths);
    }

    /// <summary>
    /// Sobrecarga simple para victoria/derrota genérica.
    /// </summary>
    public void Show(bool isVictory)
    {
        string defaultSubtitle = isVictory ? victorySurviveSubtitle : defeatOutOfLivesSubtitle;
        DisplayUI(isVictory, defaultSubtitle);
    }

    /// <summary>
    /// Sobrecarga simple con estadísticas.
    /// </summary>
    public void Show(bool isVictory, int kills, int deaths)
    {
        Show(isVictory);
        SetStats(kills, deaths);
    }

    private void DisplayUI(bool isVictory, string subtitle)
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(true);
        }

        // Liberar cursor para permitir navegación en la UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (titleText != null)
        {
            titleText.text = isVictory ? victoryTitle : defeatTitle;
            titleText.color = isVictory ? victoryColor : defeatColor;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
        }
    }

    private void SetStats(int kills, int deaths)
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(true);
        }

        if (killsValueText != null)
        {
            killsValueText.text = kills.ToString();
        }

        if (deathsValueText != null)
        {
            deathsValueText.text = deaths.ToString();
        }
    }

    /// <summary>
    /// Oculta el panel.
    /// </summary>
    public void Hide()
    {
        if (rootPanel != null)
        {
            rootPanel.SetActive(false);
        }
    }

    private void OnMainMenuClicked()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null)
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnPlayAgainClicked()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene(
                SceneManager.GetActiveScene().name, 
                LoadSceneMode.Single
            );
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Probar Victoria: Tiempo Sobrevivido")]
    private void TestVictorySurvive()
    {
        Show(MatchResultReason.SurviveTime, 14, 0);
    }

    [ContextMenu("Probar Victoria: Enemigos Eliminados")]
    private void TestVictoryAllEnemies()
    {
        Show(MatchResultReason.DefeatedAllEnemies, 25, 1);
    }

    [ContextMenu("Probar Derrota: Sin Vidas")]
    private void TestDefeatNoLives()
    {
        Show(MatchResultReason.OutOfLives, 8, 3);
    }

    [ContextMenu("Probar Derrota: Torre Destruida")]
    private void TestDefeatTower()
    {
        Show(MatchResultReason.TowerDestroyed, 11, 2);
    }

    [ContextMenu("Ocultar Pantalla")]
    private void TestHide()
    {
        Hide();
    }
#endif
}
