using System.Collections.Generic;
using UnityEngine;
using TMPro;   // Solo si usas TextMeshPro para el texto

public class BlackjackHand : MonoBehaviour
{
    [Header("Refs")]
    public CardSnapZone snapZone;       // La zona donde snappean las cartas
    public TMP_Text valueText;          // Texto en UI para mostrar el valor (opcional)

    private List<Card> cards = new List<Card>();

    private void Awake()
    {
        // Si no lo asignas a mano, intenta buscarlo en el mismo objeto
        if (snapZone == null)
            snapZone = GetComponent<CardSnapZone>();

        // Conectamos la mano con la zona de snap
        if (snapZone != null)
        {
            snapZone.blackjackHand = this;
        }
    }

    // Llamado por la zona de snap cuando una carta se coloca
    public void AddCard(Card card)
    {
        if (card == null) return;
        if (!cards.Contains(card))
        {
            cards.Add(card);
            RecalculateValue();
        }
    }

    // Llamado por la zona de snap cuando una carta se quita / devuelve al mazo
    public void RemoveCard(Card card)
    {
        if (card == null) return;
        if (cards.Remove(card))
        {
            RecalculateValue();
        }
    }

    private void RecalculateValue()
    {
        int total = CalculateBestBlackjackValue(cards);

        if (valueText != null)
        {
            valueText.text = total.ToString();
        }

        // Aquí puedes hacer lógica extra:
        // if (total > 21) -> bust
        // if (total == 21 && cards.Count == 2) -> blackjack natural, etc.
    }

    // Lógica del As = 11 o 1
    private int CalculateBestBlackjackValue(List<Card> hand)
    {
        int total = 0;
        int aceCount = 0;

        foreach (var card in hand)
        {
            int v = card.GetBlackjackValue();
            total += v;

            if (card.rank == Rank.Ace)
                aceCount++;
        }

        // Si nos pasamos de 21, vamos bajando Ases de 11 -> 1
        while (total > 21 && aceCount > 0)
        {
            total -= 10; // 11 pasa a ser 1
            aceCount--;
        }

        return total;
    }
}
