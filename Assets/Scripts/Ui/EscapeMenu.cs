using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class EscapeMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Header("--------UI--------")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Slider volumeSlider;

    private const string VolumePrefKey = "MasterVolume";

    private void Start()
    {
        menuPanel.SetActive(false);
        IsOpen = false;

        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
        AudioListener.volume = savedVolume;

        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        IsOpen = true;
        menuPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        IsOpen = false;
        menuPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(VolumePrefKey, value);
    }

    public void ExitToMainMenu()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene("menuPrincipal");
    }
}