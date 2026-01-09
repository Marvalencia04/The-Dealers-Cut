using UnityEngine;
using TMPro;

public class BlackjackScore : MonoBehaviour
{
    public CardSnapZone snapZone;
    public TMP_Text valueText;
    public bool logDebug = false;

    private int lastTotal = int.MinValue;

    //  OPTIMIZACIÓN: Usar eventos en lugar de Update
    private bool isDirty = false;

    private void Awake()
    {
        if (snapZone == null)
            snapZone = GetComponent<CardSnapZone>();
    }

    private void Start()
    {
        //  OPTIMIZACIÓN: Suscribirse a eventos personalizados si están disponibles
        // O llamar manualmente cuando sea necesario
        UpdateScore();
    }

    //  OPTIMIZACIÓN: Cambiar Update por LateUpdate y solo actualizar si hay cambios
    private void LateUpdate()
    {
        if (!isDirty)
            return;

        UpdateScore();
        isDirty = false;
    }

    //  NUEVO: Método público para marcar que necesita actualización
    public void MarkDirty()
    {
        isDirty = true;
    }

    //  NUEVO: Método público para forzar actualización inmediata
    public void UpdateScore()
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

        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        //  OPTIMIZACIÓN: Solo actualizar UI si cambió
        if (total != lastTotal)
        {
            valueText.text = total.ToString();
            if (logDebug)
                Debug.Log($"[BlackjackScore:{name}] total = {total}");
            lastTotal = total;
        }
    }
}