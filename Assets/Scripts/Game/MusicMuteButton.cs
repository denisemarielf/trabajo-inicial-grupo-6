using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicMuteButton : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonLabel;

    private void Start()
    {
        UpdateLabel();
    }

    public void OnClickToggle()
    {
        MusicManager.ToggleMute();
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (buttonLabel == null) return;
        buttonLabel.text = MusicManager.IsMuted ? "🔇" : "🔊";
    }
}