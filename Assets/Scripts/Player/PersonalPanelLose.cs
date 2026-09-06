using UnityEngine;

// Poner este script en el GameObject del panel de derrota individual
// que ya tenes dentro del Canvas de la escena (NO dentro del prefab del Player).
public class PersonalLosePanel : MonoBehaviour
{
    public static PersonalLosePanel Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}