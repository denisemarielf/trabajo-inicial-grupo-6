using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private float volume = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;

    private const string MutedPrefKey = "MusicMuted";

    private AudioSource audioSource;
    private float targetVolume;

    public static bool IsMuted { get; private set; }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = musicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        IsMuted = PlayerPrefs.GetInt(MutedPrefKey, 0) == 1;
        targetVolume = volume;
        audioSource.volume = IsMuted ? 0f : 0f; // arranca en 0 para el fade-in
    }

    private void Start()
    {
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float finalVolume = IsMuted ? 0f : targetVolume;
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, finalVolume, t / fadeInDuration);
            yield return null;
        }
        audioSource.volume = finalVolume;
    }

    public static void SetMuted(bool muted)
    {
        IsMuted = muted;
        PlayerPrefs.SetInt(MutedPrefKey, muted ? 1 : 0);

        // Aplica el cambio a cualquier MusicManager activo en la escena actual
        MusicManager current = FindAnyObjectByType<MusicManager>();
        if (current != null)
        {
            current.audioSource.volume = muted ? 0f : current.targetVolume;
        }
    }

    public static void ToggleMute()
    {
        SetMuted(!IsMuted);
    }
}