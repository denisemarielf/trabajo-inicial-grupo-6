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

    [Header("Referencia a la logica de Relay")]
    public RelayConnectionManager relayManager;

    void Start()
    {
        btnCrearPartida.onClick.AddListener(OnCrearPartida);
        btnUnirse.onClick.AddListener(OnUnirse);
        btnSalir.onClick.AddListener(OnSalir);
    }

    private void OnCrearPartida()
    {
        relayManager.CreateRelay();
    }

    private void OnUnirse()
    {
        string codigo = inputCodigoPartida.text.Trim().ToUpper();
        relayManager.JoinRelay(codigo);
    }

    private void OnSalir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}