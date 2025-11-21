using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public string gameSceneName = "GameScene";  // 你的游戏主场景名
    public GameObject configPanel;             // 可选：配置面板
    public GameObject mainMenuPanel;

    // 按钮：JUGAR
    public void Jugar()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 按钮：CONFIGURACION
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

    // 按钮：SALIR
    public void Salir()
    {
        Debug.Log("Salir del juego...");
        Application.Quit();
    }
}
