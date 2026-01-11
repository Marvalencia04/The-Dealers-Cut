using System.Collections;
using UnityEngine;

public class Giro : MonoBehaviour
{
    [Header("Reels")]
    public ReelSpinner r1;
    public ReelSpinner r2;
    public ReelSpinner r3;

    [Header("Referencias externas")]
    public SlotPrizeManager prizeManager;
    [SerializeField] private MoneyManager moneyManager;

    [Header("Día actual")]
    public int currentDay = 1;

    [Header("Timing")]
    public float delayBetweenReels = 0.15f;

    [Header("Control de tiradas")]
    [SerializeField] private int guaranteedEvery = 10;
    private int spinCounter = 0;


    public int[] reelOrder = new int[]
    {
        1,2,3, 1,0,2,3, 1,2,3, 1,2,3, 1,2,3
    };

    [Header("Calibración (shift por reel)")]
    public int shiftR1 = 0;
    public int shiftR2 = 0;
    public int shiftR3 = 0;

    [Header("Probabilidades de símbolos")]
    public int[] symbolWeights = new int[4] { 1, 3, 3, 1 };

    private void Update()
    {
        // Giro normal
        // if (Input.GetKeyDown(KeyCode.Space))
        //     IntentarGiro();

        // Forzar triples
        if (Input.GetKeyDown(KeyCode.Alpha1)) ForceTriple(0); // Triple 7
        if (Input.GetKeyDown(KeyCode.Alpha2)) ForceTriple(1); // Triple Campana
        if (Input.GetKeyDown(KeyCode.Alpha3)) ForceTriple(2); // Triple Cereza
        if (Input.GetKeyDown(KeyCode.Alpha4)) ForceTriple(3); // Triple BAR

        // Forzar combinación personalizada (ejemplo Ctrl+5)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5))
            ForceResult(0, 1, 2); // 7, Campana, Cereza
    }

    // ==========================================================
    // Giro normal
    // ==========================================================
    public void IntentarGiro()
    {
        if (moneyManager == null)
            return;

        if (!moneyManager.TryPaySlotSpin(currentDay))
        {
            Debug.Log("💸 No hay dinero suficiente para girar.");
            return;
        }

        spinCounter++;

        int[] result;

        if (spinCounter >= guaranteedEvery)
        {
            result = GenerateGuaranteedTriple();
            spinCounter = 0;

            Debug.Log("🎯 TIRADA GARANTIZADA: TRIPLE FIGURA");
        }
        else
        {
            result = GenerateWeightedArray();
        }

        Debug.Log($"🎰 Tirada {spinCounter}/{guaranteedEvery}");
        PlayRequested(result);
    }
    private int[] GenerateGuaranteedTriple()
    {
        int symbol = WeightedRandom(symbolWeights);
        return new int[] { symbol, symbol, symbol };
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

    // ==========================================================
    // Forzar resultados
    // ==========================================================

    /// <summary>
    /// Fuerza un triple de un mismo símbolo (0..3)
    /// </summary>
    public void ForceTriple(int symbol)
    {
        int[] forced = new int[3] { symbol, symbol, symbol };
        Debug.Log($"🎯 Forzando triple: [{symbol},{symbol},{symbol}]");
        PlayRequested(forced);
    }

    /// <summary>
    /// Fuerza un resultado específico de 3 símbolos
    /// </summary>
    public void ForceResult(int symbol1, int symbol2, int symbol3)
    {
        int[] forced = new int[3] { symbol1, symbol2, symbol3 };
        Debug.Log($"🎯 Forzando resultado: [{symbol1},{symbol2},{symbol3}]");
        PlayRequested(forced);
    }

    // ==========================================================
    // Animación de rodillos
    // ==========================================================
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
        int idx1 = FindNextIndexForSymbol(r1, req[0], shiftR1);
        r1.SpinToIndex(idx1);
        yield return new WaitForSeconds(delayBetweenReels);

        int idx2 = FindNextIndexForSymbol(r2, req[1], shiftR2);
        r2.SpinToIndex(idx2);
        yield return new WaitForSeconds(delayBetweenReels);

        int idx3 = FindNextIndexForSymbol(r3, req[2], shiftR3);
        r3.SpinToIndex(idx3);

        float timeout = Mathf.Max(r1.spinDuration, r2.spinDuration, r3.spinDuration) + 2f;
        float elapsed = 0f;

        /*while ((r1.IsSpinning || r2.IsSpinning || r3.IsSpinning) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }*/

        if (elapsed >= timeout)
            Debug.LogWarning("Timeout esperando rodillos.");

        if (prizeManager != null)
        {
            Debug.Log("Evaluando resultado final: [" + string.Join(", ", req) + "]");
            prizeManager.EvaluarResultado(req);
        }
        else
        {
            Debug.LogWarning("No hay PrizeManager asignado.");
        }
    }

    // ==========================================================
    // Cálculo del siguiente índice para el rodillo
    // ==========================================================
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
