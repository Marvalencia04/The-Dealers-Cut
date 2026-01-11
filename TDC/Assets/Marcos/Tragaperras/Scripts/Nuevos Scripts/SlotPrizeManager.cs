using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Evalúa combinaciones ganadoras en la tragaperras
/// y recompensa con trampas según el triple obtenido.
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

    [Header("UI Premio (opcional)")]
    [Tooltip("Imagen donde se mostrará el sprite de la trampa ganada")]
    [SerializeField] private Image rewardTrapImage;

    [Header("Texto de resultado")]
    [Tooltip("Texto que muestra si has ganado o no")]
    [SerializeField] private TMP_Text resultText;

    [Tooltip("Duración antes de comenzar a apagar los efectos.")]
    public float stopEffectsAfter = 5f;

    [Header("Audio")]
    public AudioSource audioGanar;

    private Coroutine activeCoroutine;

    private void Start()
    {
        if (slotMachine == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay referencia a Giro.");

        if (TrampasManager.Instance == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay TrampasManager en escena.");

        if (resultText == null)
            Debug.LogWarning("⚠️ SlotPrizeManager: no hay referencia a resultText.");
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

        // ⏳ Esperar animaciones
        Debug.Log("⏳ Esperando 4 segundos antes de evaluar el resultado...");
        yield return new WaitForSeconds(4f);

        // 🔹 CASO 1: Triple 7 → Legendaria
        if (a == 0 && b == 0 && c == 0)
        {
            OnWinTrap(TrapRarity.Legendaria, "Triple 7");
            yield break;
        }

        // 🔹 CASO 2: Tres iguales
        if (a == b && b == c)
        {
            switch (a)
            {
                case 1: // Campana
                    OnWinTrap(TrapRarity.Comun, "Triple Campana");
                    break;

                case 2: // Cereza
                    OnWinTrap(TrapRarity.Epica, "Triple Cereza");
                    break;

                case 3: // BAR
                    OnWinTrap(TrapRarity.Rara, "Triple BAR");
                    break;
            }
            yield break;
        }

        // 🔹 CASO 3: Sin premio
        OnLose();
    }

    // ==========================================================
    // 🎉 Victoria
    // ==========================================================

    private void OnWinTrap(TrapRarity rarity, string motivo)
    {
        Debug.Log($"🏅 Premio obtenido: {rarity} ({motivo})");

        // 📝 Texto
        if (resultText != null)
            resultText.text = "Enhorabuena";

        // 🎁 Trampas
        if (TrampasManager.Instance != null)
        {
            TrapType rewardedTrap = TrampasManager.Instance.RecoverTrap(rarity);
            TrampasManager.Instance.LogUsosActuales();

            Sprite trapSprite = TrampasManager.Instance.GetTrapSprite(rewardedTrap);

            if (rewardTrapImage != null)
            {
                rewardTrapImage.sprite = trapSprite;
                rewardTrapImage.gameObject.SetActive(trapSprite != null);
            }

            Debug.Log($"🎁 Trampa obtenida: {rewardedTrap}");
        }

        // 🔊 Sonido
        if (soundManager != null)
            soundManager.OnWin();

        // 🎊 FX
        if (confettiFX != null) confettiFX.Play(true);
        if (coinsFX != null) coinsFX.Play(true);
        if (audioGanar != null) audioGanar.Play();

        // ⏹ Apagar FX
        if (stopEffectsAfter > 0)
            activeCoroutine = StartCoroutine(FadeOutEffects());
    }

    // ==========================================================
    // 😕 Derrota
    // ==========================================================

    private void OnLose()
    {
        Debug.Log("😕 Mala suerte");

        // 📝 Texto
        if (resultText != null)
            resultText.text = "Mala suerte";

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
        }

        if (coinsFX != null)
        {
            var em = coinsFX.emission;
            em.enabled = false;
        }

        yield return new WaitForSeconds(2f);

        if (confettiFX != null) confettiFX.Clear();
        if (coinsFX != null) coinsFX.Clear();

        activeCoroutine = null;
    }
}
