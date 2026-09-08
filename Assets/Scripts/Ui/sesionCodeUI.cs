
using TMPro;
using UnityEngine;

public class sesionCodeUI : MonoBehaviour
{
    [SerializeField] private TMP_Text sesionCode;
    void Start()
    {
        findCodeSesion();
    }

    private void findCodeSesion()
    {
        if (sesionCode != null)
        {
            if (!string.IsNullOrEmpty(RelayConnectionManager.CodigoPartidaActual))
            {
                sesionCode.text = "Code: " + RelayConnectionManager.CodigoPartidaActual.ToLower();
            }
            else
            {
                sesionCode.text = " ";
            }
        }
    }
    }
