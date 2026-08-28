using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenu;
   
   

    public void QuitGame()
    {
        Application.Quit();

    }
    public void PlayGame()
    {
        SceneManager.LoadScene("escenaPrincipal");

    }


}
