using UnityEngine;

public class StartMenu : MonoBehaviour
{
   
    public GameObject configPanel;
    public GameObject mainMenuPanel;
    public GameObject locomotion;

    public void Jugar()
    {
        mainMenuPanel.SetActive(false);
        locomotion.SetActive(true);
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