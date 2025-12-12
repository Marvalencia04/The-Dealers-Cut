using UnityEngine;

/// <summary>
/// Controla los sonidos de la tragaperras.
/// Se encarga de reproducir el sonido de giro y de detenerlo cuando acaba.
/// </summary>
public class SlotSoundManager : MonoBehaviour
{
    [Header("Sonidos")]
    public AudioSource spinLoopSource;     // sonido continuo mientras giran los rodillos
    public AudioClip spinStartClip;        // sonido corto al empezar el giro (opcional)
    public AudioClip spinStopClip;         // sonido corto al parar cada rodillo
    public AudioClip winClip;              // sonido de premio
    public AudioClip loseClip;             // sonido de fallo

    [Header("Configuración")]
    public float stopDelayFade = 0.3f;     // fade suave al detener el sonido de giro

    int spinningReels = 0; // contador de rodillos activos

    //Llamar cuando un rodillo empieza a girar
    public void OnReelStart()
    {
        spinningReels++;

        if (spinningReels == 1) // si es el primero, arranca el sonido global
        {
            if (spinStartClip)
                AudioSource.PlayClipAtPoint(spinStartClip, Camera.main.transform.position);

            if (spinLoopSource && !spinLoopSource.isPlaying)
                spinLoopSource.Play();
        }
    }

    //Llamar cuando un rodillo termina
    public void OnReelStop()
    {
        spinningReels = Mathf.Max(0, spinningReels - 1);

        if (spinStopClip)
            AudioSource.PlayClipAtPoint(spinStopClip, Camera.main.transform.position);

        // Si ya no hay rodillos girando → parar sonido principal
        if (spinningReels == 0 && spinLoopSource != null)
            StartCoroutine(FadeOut(spinLoopSource, stopDelayFade));
    }

    //Desde SlotPrizeManager
    public void OnWin()
    {
        if (winClip)
            AudioSource.PlayClipAtPoint(winClip, Camera.main.transform.position);
    }

    public void OnLose()
    {
        if (loseClip)
            AudioSource.PlayClipAtPoint(loseClip, Camera.main.transform.position);
    }

    // Suaviza el apagado del sonido
    private System.Collections.IEnumerator FadeOut(AudioSource src, float fadeTime)
    {
        if (src == null) yield break;
        float startVol = src.volume;
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / fadeTime);
            yield return null;
        }

        src.Stop();
        src.volume = startVol; // restaurar
    }
}
