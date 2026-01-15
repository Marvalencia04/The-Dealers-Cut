using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
   
    public GameObject configPanel;
    public GameObject mainMenuPanel;
    public GameManager gameManager;

    [Header("Teleport al empezar")]
    [SerializeField] private Transform startTeleportTarget;
    [SerializeField] private Transform playerRoot;


    public void Jugar()
    {
        mainMenuPanel.SetActive(false);
        // Teletransportar jugador
        if (playerRoot != null && startTeleportTarget != null)
        {
            playerRoot.position = startTeleportTarget.position;
            playerRoot.rotation = startTeleportTarget.rotation * Quaternion.Euler(0f, -90f, 0f);
        }
        gameManager.StartGame();

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