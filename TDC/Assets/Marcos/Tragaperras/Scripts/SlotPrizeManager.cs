using UnityEngine;
using System.Collections;

/// <summary>
/// Evalúa combinaciones ganadoras en la tragaperras,
/// activa efectos, sonidos y actualiza el saldo del jugador.
/// </summary>
public class SlotPrizeManager : MonoBehaviour
{
    [Header("Referencia al controlador principal")]
    public Giro slotMachine;

    [Header("Sistema de monedas")]
    public SlotCurrencyManager currencyManager; // 💰 Nuevo sistema

    [Header("Premios configurables")]
    public int premioTresIguales = 100;

    public int premioTriple7 = 500;
    public string mensajeSinPremio = "Sigue intentando...";

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
        if (currencyManager == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay referencia a SlotCurrencyManager.");
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

        // 🔹 CASO 1: Triple 7
        if (a == 0 && b == 0 && c == 0)
        {
            Debug.Log($"🎉 ¡Triple 7! Premio: {premioTriple7}");
            OnWin(premioTriple7, "¡Triple 7!", true);
            yield break;
        }

        // 🔹 CASO 2: Tres iguales
        if (a == b && b == c)
        {
            Debug.Log($"🎉 Tres iguales ({a}) → Premio: {premioTresIguales}");
            OnWin(premioTresIguales, "Tres iguales", false);
            yield break;
        }

        // 🔹 CASO 3: Sin premio
        Debug.Log(mensajeSinPremio);
        OnLose();
    }


    // ==========================================================
    // 🔔 Reacciones a victoria o derrota
    // ==========================================================

    protected virtual async void OnWin(int cantidad, string tipo, bool isTriple7)
    {
        Debug.Log($"🏅 Ganaste {cantidad} monedas por: {tipo}");

       

        // 💰 Añadir monedas al jugador
        if (currencyManager != null)
            currencyManager.AñadirPremio(cantidad);

        // 🔊 Sonido de victoria
        if (soundManager != null)
            soundManager.OnWin();

        // 🔄 Cancelar efectos anteriores si los hubiera
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        // 🎊 Confeti (para cualquier victoria)
        if (confettiFX && isTriple7 != null)
        {
            
            var em = confettiFX.emission;
            em.enabled = true;
            confettiFX.Play(true);
            Debug.Log("🎉 Confeti activado.");
            var emC = coinsFX.emission;
            emC.enabled = true;
            coinsFX.Play(true);
            Debug.Log("💰 Monedas activadas.");

            if (audioGanar != null)
            {
                audioGanar.Play();
                Debug.Log("🔊 Sonido de ganar reproducido.");
            }
            else
            {
                Debug.LogWarning("⚠️ No se asignó el AudioSource 'audioGanar' en el inspector.");
            }

        }

        // 💰 Monedas (solo para triple 7)'''
        //if (isTriple7 && coinsFX != null)
        //{
        //    var em = coinsFX.emission;
        //    em.enabled = true;
         //   coinsFX.Play(true);
         //   Debug.Log("💰 Monedas activadas.");
       // }

        if (stopEffectsAfter > 0)
            activeCoroutine = StartCoroutine(FadeOutEffects());
    }

    protected virtual void OnLose()
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

        // Esperar un poco a que las partículas se disipen
        yield return new WaitForSeconds(2f);

        if (confettiFX != null) confettiFX.Clear();
        if (coinsFX != null) coinsFX.Clear();

        activeCoroutine = null;
    }
}
