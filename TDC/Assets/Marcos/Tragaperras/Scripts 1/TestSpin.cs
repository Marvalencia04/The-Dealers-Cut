using System.Collections;
using UnityEngine;

public class TestSpin : MonoBehaviour
{
    // Asigna tus 3 cilindros con ReelSpinner
    public ReelSpinner r1; // Cylinder.001
    public ReelSpinner r2; // Cylinder.002
    public ReelSpinner r3; // Cylinder.003

    // Entrada (0=7, 1=Campana, 2=Cereza, 3=BAR)
    public int[] testArray = new int[3] { 0, 1, 2 };

    [Header("Timing")]
    public float delayBetweenReels = 0.15f;

    // ORDEN del reel (16 casillas) empezando en el índice 0
    // campana, cereza, BAR, campana, 7, cereza, BAR, campana, cereza, BAR, campana, cereza, BAR, campana, cereza, BAR
    // Mapeo: 0=7, 1=Campana, 2=Cereza, 3=BAR
    // => [1,2,3, 1,0,2,3, 1,2,3, 1,2,3, 1,2,3]
    public int[] reelOrder = new int[]
    {
        1,2,3, 1,0,2,3, 1,2,3, 1,2,3, 1,2,3
    };

    [Header("Calibración (shift por reel)")]
    [Tooltip("Cuántas casillas está rotado el orden real respecto a index=0 visual")]
    public int shiftR1 = 0;
    public int shiftR2 = 0;
    public int shiftR3 = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            PlayRequested(testArray);
    }

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
    }

    // Busca la PRÓXIMA aparición del símbolo, teniendo en cuenta el shift del reel
    int FindNextIndexForSymbol(ReelSpinner reel, int symbol, int shift)
    {
        int items = Mathf.Max(1, reel.items);
        int from = reel.GetApproxIndex();

        // Normaliza shift
        shift = ((shift % items) + items) % items;

        // Queremos un índice i tal que reelOrder[(i + shift) % items] == symbol
        for (int step = 1; step <= items; step++)
        {
            int i = (from + step) % items;
            if (reelOrder[(i + shift) % items] == symbol)
                return i;
        }

        // Fallback (por si acaso)
        for (int i = 0; i < items; i++)
            if (reelOrder[(i + shift) % items] == symbol) return i;

        return from;
    }
}
