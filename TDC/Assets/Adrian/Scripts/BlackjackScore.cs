using UnityEngine;
using TMPro;

public class BlackjackScore : MonoBehaviour
{
    public CardSnapZone snapZone;
    public TMP_Text valueText;
    public bool logDebug = false;

    private int lastTotal = int.MinValue;

    private void Awake()
    {
        if (snapZone == null)
            snapZone = GetComponent<CardSnapZone>();
    }

    private void Update()
    {
        if (snapZone == null || valueText == null)
            return;

        int total = 0;
        int aceCount = 0;

        foreach (var card in snapZone.GetCurrentCards())
        {
            if (card == null) continue;

            int v = card.GetBlackjackValue();
            total += v;

            if (card.rank == Rank.Ace)
                aceCount++;
        }

        // Ajuste de Ases (11 -> 1) si nos pasamos de 21
        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        if (total != lastTotal)
        {
            valueText.text = total.ToString();
            if (logDebug)
                Debug.Log($"[BlackjackScore:{name}] total = {total}");
            lastTotal = total;
        }
    }
}
