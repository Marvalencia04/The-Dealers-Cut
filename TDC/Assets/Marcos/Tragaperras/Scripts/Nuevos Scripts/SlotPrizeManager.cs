using UnityEngine;
using System.Collections;

/// <summary>
/// Evalúa combinaciones ganadoras en la tragaperras y recompensa con trampas según el triple.
/// </summary>
public class SlotPrizeManager : MonoBehaviour
{
    [Header("Referencia al controlador principal")]
    public Giro slotMachine;

    [Header("Gestión de sonidos (opcional)")]
    public SlotSoundManager soundManager;

    [Header("Efectos visuales (opcional)")]
    public ParticleSystem confettiFX;
    public ParticleSystem coinsFX;

    [Tooltip("Duración antes de comenzar a apagar los efectos.")]
    public float stopEffectsAfter = 5f;

    private Coroutine activeCoroutine;
    public AudioSource audioGanar;

    private void Start()
    {
        if (slotMachine == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay referencia a Giro.");
        if (TrampasManager.Instance == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay referencia a TrampasManager en escena.");
    }

    // ==========================================================
    // 🏆 Evaluación del resultado
    // ==========================================================
    public void EvaluarResultado(int[] resultado)
    {
        StartCoroutine(EvaluarResultadoConRetraso(resultado));
    }

    private IEnumerator EvaluarResultadoConRetraso(int[] resultado)
    {
        if (resultado == null || resultado.Length != 3)
        {
            Debug.LogError("❌ Resultado inválido recibido por SlotPrizeManager.");
            yield break;
        }

        int a = resultado[0];
        int b = resultado[1];
        int c = resultado[2];

        // ⏳ Esperar 4 segundos antes de evaluar el resultado
        Debug.Log("⏳ Esperando 4 segundos antes de evaluar el resultado...");
        yield return new WaitForSeconds(4f);

        // 🔹 CASO 1: Triple 7 → Trampa Legendaria
        if (a == 0 && b == 0 && c == 0)
        {
            Debug.Log("🎉 Triple 7 → recompensa Trampa Legendaria");
            OnWinTrap(TrapRarity.Legendaria, "¡Triple 7!");
            yield break;
        }

        // 🔹 CASO 2: Tres iguales (otros símbolos)
        if (a == b && b == c)
        {
            switch (a)
            {
                case 1: // Campana
                    Debug.Log("🎉 Triple Campana → recompensa Trampa Comun");
                    OnWinTrap(TrapRarity.Comun, "Triple Campana");
                    break;
                case 2: // Cereza
                    Debug.Log("🎉 Triple Cereza → recompensa Trampa Epica");
                    OnWinTrap(TrapRarity.Epica, "Triple Cereza");
                    break;
                case 3: // BAR
                    Debug.Log("🎉 Triple BAR → recompensa Trampa Rara");
                    OnWinTrap(TrapRarity.Rara, "Triple BAR");
                    break;
            }
            yield break;
        }

        // 🔹 CASO 3: Sin premio
        Debug.Log("Sigue intentando...");
        OnLose();
    }

    // ==========================================================
    // 🔔 Reacciones a victoria o derrota
    // ==========================================================
    protected void OnWinTrap(TrapRarity rarity, string tipo)
    {
        Debug.Log($"🏅 Has ganado una trampa {rarity} por: {tipo}");

        // 💎 Añadir trampa al jugador
        if (TrampasManager.Instance != null)
        {
            TrampasManager.Instance.RecoverTrap(rarity);
            TrampasManager.Instance.LogUsosActuales();

        }
        else
        {
            Debug.LogWarning("⚠️ No hay TrampasManager en escena.");
        }

        // 🔊 Sonido de victoria
        if (soundManager != null)
            soundManager.OnWin();

        // 🎊 Efectos visuales
        if (confettiFX != null)
            confettiFX.Play(true);
        if (coinsFX != null)
            coinsFX.Play(true);

        // 🔉 Audio de ganar
        if (audioGanar != null)
            audioGanar.Play();

        // 💨 Detener efectos después de stopEffectsAfter
        if (stopEffectsAfter > 0)
            activeCoroutine = StartCoroutine(FadeOutEffects());
    }


    protected void OnLose()
    {
        if (soundManager != null)
            soundManager.OnLose();
    }

    // ==========================================================
    // 🌈 Fade out de efectos
    // ==========================================================
    private IEnumerator FadeOutEffects()
    {
        yield return new WaitForSeconds(stopEffectsAfter);

        if (confettiFX != null)
        {
            var em = confettiFX.emission;
            em.enabled = false;
            Debug.Log("⏹ Deteniendo confeti.");
        }

        if (coinsFX != null)
        {
            var em = coinsFX.emission;
            em.enabled = false;
            Debug.Log("⏹ Deteniendo monedas.");
        }

        // Esperar a que las partículas se disipen
        yield return new WaitForSeconds(2f);

        if (confettiFX != null) confettiFX.Clear();
        if (coinsFX != null) coinsFX.Clear();

        activeCoroutine = null;
    }
}
