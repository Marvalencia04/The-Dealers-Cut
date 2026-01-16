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

    [Header("Bloqueo palanca")]
    [SerializeField] private Collider leverCollider; // asigna el collider de la palanca aquí

    private bool isSpinning = false;
    private Coroutine spinRoutine;

    // ✅ ESTE ES EL QUE LLAMA EL ANIMATION EVENT
    public void IntentarGiro()
    {
        // Si el Animation Event se dispara más de una vez, lo ignoramos
        if (isSpinning) return;

        isSpinning = true;

        if (leverCollider != null)
            leverCollider.enabled = false;

        if (moneyManager == null)
        {
            EndSpin();
            return;
        }

        if (!moneyManager.TryPaySlotSpin(currentDay))
        {
            Debug.Log("💸 No hay dinero suficiente para girar.");
            EndSpin();
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

        if (spinRoutine != null) StopCoroutine(spinRoutine);
        spinRoutine = StartCoroutine(SpinAllSequential(result));
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

    // ✅ Espera fija segura
    float waitTime = Mathf.Max(r1.spinDuration, r2.spinDuration, r3.spinDuration) + 0.1f;
    yield return new WaitForSeconds(waitTime);

    if (prizeManager != null)
        prizeManager.EvaluarResultado(req);

    EndSpin();
}


    private void EndSpin()
    {
        isSpinning = false;

        if (leverCollider != null)
            leverCollider.enabled = true;
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
