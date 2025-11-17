using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public string gameSceneName = "GameScene";  // 你的游戏主场景名
    public GameObject configPanel;             // 可选：配置面板

    // 按钮：JUGAR
    public void Jugar()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // 按钮：CONFIGURACION
    public void Configuracion()
    {
        if (configPanel != null)
        {
            configPanel.SetActive(!configPanel.activeSelf);
        }
        else
        {
            Debug.Log("Abrir configuracion (aún sin panel asignado)");
        }
    }

    // 按钮：SALIR
    public void Salir()
    {
        Debug.Log("Salir del juego...");
        Application.Quit();
    }
}
