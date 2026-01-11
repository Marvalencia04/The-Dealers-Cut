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

        // ✨ SONIDO AL INICIO (en cuanto aparece el guardia)
        if (audioSource && sfx) audioSource.PlayOneShot(sfx);

        // Espera en tiempo real
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
            Debug.LogWarning("Este test solo funciona en PLAY MODE. Presiona el botón ▶Play primero.");
            return;
        }
        Execute();
    }
#endif
}