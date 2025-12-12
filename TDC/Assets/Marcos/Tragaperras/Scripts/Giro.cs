using System.Collections;
using UnityEngine;

public class Giro : MonoBehaviour
{
    [Header("Reels")]
    public ReelSpinner r1; // Cylinder.001
    public ReelSpinner r2; // Cylinder.002
    public ReelSpinner r3; // Cylinder.003

    [Header("Referencias externas")]
    public SlotCurrencyManager currencyManager;  // 💰 Sustituye a CurrencyController
    public SlotPrizeManager prizeManager;        // 🎁 Evaluador de premios


    [Header("Timing")]
    public float delayBetweenReels = 0.15f;

    // ORDEN del reel (16 casillas)
    // Mapeo: 0=7, 1=Campana, 2=Cereza, 3=BAR
    public int[] reelOrder = new int[]
    {
        1,2,3, 1,0,2,3, 1,2,3, 1,2,3, 1,2,3
    };

    [Header("Calibración (shift por reel)")]
    public int shiftR1 = 0;
    public int shiftR2 = 0;
    public int shiftR3 = 0;

    [Header("Probabilidades de símbolos")]
    [Tooltip("Pesos de aparición para cada símbolo (la suma puede ser cualquier número)")]
    public int[] symbolWeights = new int[4] { 1, 3, 3, 1 };
    // Ejemplo: [7, Campana, Cereza, BAR] => 7 raro, campana y cereza comunes, BAR raro.

    // ============================================================
    // 🎮 Control principal
    // ============================================================

    private void Update()
    {
        // Inicia una jugada con SPACE o clic de prueba
        if (Input.GetKeyDown(KeyCode.Space))
            IntentarGiro();

        // Depuración: forzar triples con teclas
        if (Input.GetKeyDown(KeyCode.Alpha1)) ForceResult(0); // Triple 7
        if (Input.GetKeyDown(KeyCode.Alpha2)) ForceResult(1); // Triple Campana
        if (Input.GetKeyDown(KeyCode.Alpha3)) ForceResult(2); // Triple Cereza
        if (Input.GetKeyDown(KeyCode.Alpha4)) ForceResult(3); // Triple BAR
    }

    // ============================================================
    // 💰 Control de monedas y ejecución del giro
    // ============================================================

    public void IntentarGiro()
    {
        if (currencyManager == null)
        {
            Debug.LogWarning("⚠️ No hay SlotCurrencyManager asignado.");
            return;
        }

        // Resta monedas antes de girar
        if (!currencyManager.RestarCostoJugada())
        {
            Debug.Log("❌ No hay suficientes monedas para jugar.");
            return;
        }

        // Si hay monedas → genera resultado y gira
        int[] generated = GenerateWeightedArray();
        PlayRequested(generated);
    }

    // ============================================================
    // 🎯 Métodos de resultado
    // ============================================================

    void ForceResult(int symbol)
    {
        int[] forced = new int[3] { symbol, symbol, symbol };
        Debug.Log($"🎯 Forzando resultado: [{symbol},{symbol},{symbol}]");
        PlayRequested(forced);
    }

    public int[] GenerateWeightedArray()
    {
        int[] arr = new int[3];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = WeightedRandom(symbolWeights);

        Debug.Log("Resultado generado: [" + string.Join(", ", arr) + "]");
        return arr;
    }

    int WeightedRandom(int[] weights)
    {
        int total = 0;
        foreach (var w in weights) total += Mathf.Max(0, w);
        if (total == 0) return 0;

        int r = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (r < weights[i]) return i;
            r -= weights[i];
        }
        return 0;
    }

    // ============================================================
    // 🎞️ Control de animaciones y sincronización
    // ============================================================

    public void PlayRequested(int[] requestSymbols)
    {
        if (requestSymbols == null || requestSymbols.Length != 3)
        {
            Debug.LogError("PlayRequested necesita un array de 3 símbolos [0..3].");
            return;
        }

        StartCoroutine(SpinAllSequential(requestSymbols));
    }

    IEnumerator SpinAllSequential(int[] req)
    {
        // Reel 1
        int idx1 = FindNextIndexForSymbol(r1, req[0], shiftR1);
        r1.SpinToIndex(idx1);
        yield return new WaitForSeconds(delayBetweenReels);

        // Reel 2
        int idx2 = FindNextIndexForSymbol(r2, req[1], shiftR2);
        r2.SpinToIndex(idx2);
        yield return new WaitForSeconds(delayBetweenReels);

        // Reel 3
        int idx3 = FindNextIndexForSymbol(r3, req[2], shiftR3);
        r3.SpinToIndex(idx3);

        // Esperar hasta que los 3 terminen
        float timeout = Mathf.Max(r1.spinDuration, r2.spinDuration, r3.spinDuration) + 2f;
        float elapsed = 0f;

      /* while ((r1.IsSpinning || r2.IsSpinning || r3.IsSpinning) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }*/

        if (elapsed >= timeout)
            Debug.LogWarning("Timeout esperando rodillos.");

        // Evaluar resultado
        if (prizeManager != null)
        {
            Debug.Log("Evaluando resultado final: [" + string.Join(", ", req) + "]");
            prizeManager.EvaluarResultado(req);
        }
        else
        {
            Debug.LogWarning("No hay PrizeManager asignado, no se evalúa el resultado.");
        }
    }

    int FindNextIndexForSymbol(ReelSpinner reel, int symbol, int shift)
    {
        int items = Mathf.Max(1, reel.items);
        int from = reel.GetApproxIndex();
        shift = ((shift % items) + items) % items;

        for (int step = 1; step <= items; step++)
        {
            int i = (from + step) % items;
            if (reelOrder[(i + shift) % items] == symbol)
                return i;
        }

        for (int i = 0; i < items; i++)
            if (reelOrder[(i + shift) % items] == symbol)
                return i;

        return from;
    }
}
