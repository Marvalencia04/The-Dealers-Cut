using System.Collections;
using UnityEngine;

public class SeatGuardKill : MonoBehaviour
{
    [Header("Grupo de NPCs del asiento")]
    [SerializeField] private NPCGroupSeat npcGroup;

    [Header("Guardia detrás (empieza desactivado)")]
    [SerializeField] private GameObject guard;

    [Header("Timing")]
    [SerializeField] private float visibleTime = 1.7f; // pon 1.5 - 2.0

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sfx;

    [Tooltip("Retraso ANTES de reproducir el sonido (segundos)")]
    [SerializeField] private float soundDelayBeforePlay = 0.5f; // ✅ lo que pedías

    [Tooltip("Si es true, espera a que termine el clip (sfx.length) después de reproducirlo")]
    [SerializeField] private bool waitForSfxToFinish = false;

    private bool running;

    private void Reset()
    {
        if (!npcGroup) npcGroup = GetComponent<NPCGroupSeat>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    public void Execute()
    {
        if (running) return;
        if (!npcGroup || !guard) return;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        running = true;

        // Capturamos el NPC activo en ESTE momento
        GameObject activeNpc = npcGroup.GetActiveNPC();
        if (!activeNpc)
        {
            running = false;
            yield break;
        }

        // Aparece el guardia
        guard.SetActive(true);

        // ⏳ Delay ANTES del sonido
        if (soundDelayBeforePlay > 0f)
            yield return new WaitForSecondsRealtime(soundDelayBeforePlay);

        // 🔊 Reproducir sonido
        if (audioSource && sfx)
        {
            audioSource.PlayOneShot(sfx);

            // ⏳ Esperar a que termine (opcional)
            if (waitForSfxToFinish)
                yield return new WaitForSecondsRealtime(sfx.length);
        }

        // ⏱️ Tiempo visible del guardia (independiente del sonido)
        float elapsed = 0f;
        while (elapsed < visibleTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Desaparece el NPC activo y el guardia
        activeNpc.SetActive(false);
        guard.SetActive(false);

        running = false;
    }

#if UNITY_EDITOR
    [ContextMenu("TEST: Execute")]
    private void TestExecute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Este test solo funciona en PLAY MODE. Presiona ▶ Play.");
            return;
        }
        Execute();
    }
#endif
}
