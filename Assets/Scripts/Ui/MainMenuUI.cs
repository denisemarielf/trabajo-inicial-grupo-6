using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button btnCrearPartida;
    public Button btnUnirse;
    public Button btnSalir;
    public TMP_InputField inputCodigoPartida;
    public TMP_Text statusText;
    public AudioSource audioSource;
    public AudioClip clickSound;
    [Header("Referencia a Lobby")]
    public LobbyManager lobbyManager;

void Start()
{Cursor.lockState = CursorLockMode.None;
Cursor.visible = true;


    btnCrearPartida.onClick.AddListener(OnCrearPartida);
    btnUnirse.onClick.AddListener(OnUnirse);
    btnSalir.onClick.AddListener(OnSalir);
}

private void OnCrearPartida()
{
    SetButtonsInteractable(false);

    if (LobbyManager.Instance == null)
    {
        Debug.LogError("No existe LobbyManager.");
        SetButtonsInteractable(true);
        return;
    }
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        LobbyManager.Instance.CrearLobby();
}

private void OnUnirse()
{
    string codigo = inputCodigoPartida.text.Trim().ToUpper();

    SetButtonsInteractable(false);

    if (LobbyManager.Instance == null)
    {
        Debug.LogError("No existe LobbyManager.");
        SetButtonsInteractable(true);
        return;
    }

        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

        LobbyManager.Instance.UnirseLobby(codigo);
}

    private void OnSalir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetButtonsInteractable(bool interactable)
    {
        btnCrearPartida.interactable = interactable;
        btnUnirse.interactable = interactable;
    }
}