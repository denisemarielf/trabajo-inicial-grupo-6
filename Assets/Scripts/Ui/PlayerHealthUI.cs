using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;       // La imagen "Fill" del Slider
    [SerializeField] private TMP_Text healthText;    // Opcional: "80 / 100"

    [Header("--------Color--------")]
    [SerializeField] private Color fullHealthColor = new Color(0.2f, 0.8f, 0.2f); // Verde
    [SerializeField] private Color lowHealthColor = new Color(0.8f, 0.15f, 0.15f); // Rojo
    [SerializeField] private bool useColorGradient = true; // false = siempre verde, solo se vacía

    public void UpdateHealthBar(float current, float max)
    {
        Debug.Log($"[PlayerHealthUI] UpdateHealthBar llamado: {current}/{max} — activo en hierarchy: {gameObject.activeInHierarchy}");
        if (healthSlider == null) return;

        float percent = max > 0 ? current / max : 0f;
        healthSlider.value = percent;

        if (fillImage != null)
        {
            fillImage.color = useColorGradient
                ? Color.Lerp(lowHealthColor, fullHealthColor, percent)
                : fullHealthColor;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }
    }
}