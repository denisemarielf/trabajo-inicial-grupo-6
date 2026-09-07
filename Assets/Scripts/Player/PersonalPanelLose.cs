using System.Collections;
using UnityEngine;

// Poner este script en el GameObject del panel de derrota individual
// que ya tenes dentro del Canvas de la escena (NO dentro del prefab del Player).
public class PersonalLosePanel : MonoBehaviour
{
    public static PersonalLosePanel Instance { get; private set; }

    [SerializeField] private float visibleDuration = 3f;

    private Coroutine hideRoutine;

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Si ya habia un ocultado programado (por ejemplo, si Show() se llama dos veces),
        // lo cancelamos para reiniciar el conteo de los 5 segundos.
        if (hideRoutine != null) StopCoroutine(hideRoutine);
        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(visibleDuration);
        gameObject.SetActive(false);
        hideRoutine = null;
    }
}