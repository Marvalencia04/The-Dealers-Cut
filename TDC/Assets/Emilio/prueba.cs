using UnityEngine;
using UnityEngine.SceneManagement;

public class prueba : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void VideoTerminado()
    {
        SceneManager.LoadSceneAsync("Mainmenu");
    }
}
