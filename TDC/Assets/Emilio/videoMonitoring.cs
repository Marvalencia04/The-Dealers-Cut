
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
public class videoMonitoring : MonoBehaviour
{
    VideoPlayer video;
    void Start()
    {
        video = this.GetComponent<UnityEngine.Video.VideoPlayer>();
        video.loopPointReached += VideoTerminado;
    }
    void VideoTerminado(UnityEngine.Video.VideoPlayer vp)
    {
        SceneManager.LoadSceneAsync("Mainmenu");
    }
}