using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
   
    public GameObject configPanel;
    public GameObject mainMenuPanel;

    public void Jugar()
    {
        mainMenuPanel.SetActive(false);
    }

    public void Video()
    {
        SceneManager.LoadSceneAsync("Video360");
    }

    public void AbrirConfiguracion()
    {
        mainMenuPanel.SetActive(false);
        configPanel.SetActive(true);
    }
    public void VolverAlMenu()
    {
        configPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void Salir()
    {
        Debug.Log("Salir del juego...");
        Application.Quit();
    }
}