using System.Collections.Generic;
using UnityEngine;

public class TableCardCleaner : MonoBehaviour
{
    [Header("Mazo al que devolvemos las cartas")]
    [SerializeField] private DeckXR deck;

    [Header("Zonas / Padres donde están las cartas (arrastrar aquí)")]
    [Tooltip("Mete aquí PlayerZones, DealerZone, Discard, etc. donde cuelgan cartas como hijos.")]
    [SerializeField] private List<Transform> cardParents = new();

    [Header("Filtro opcional")]
    [Tooltip("Si tus cartas tienen tag (ej: 'Card'), ponlo aquí para evitar tocar otros objetos. Si lo dejas vacío, se intentará detectar por componente Card.")]
    [SerializeField] private string cardTag = "";

    [Tooltip("Si está activado, busca cartas también en hijos más profundos. Si no, solo en hijos directos.")]
    [SerializeField] private bool includeChildren = true;

    public void ReturnAllToDeck()
    {
        if (deck == null)
        {
            Debug.LogWarning("[TableCardReturner] No hay DeckXR asignado.");
            return;
        }

        // 1) Recolectar cartas sin modificar jerarquía mientras iteramos
        var cards = new HashSet<Card>();

        foreach (var parent in cardParents)
        {
            if (parent == null) continue;

            if (includeChildren)
            {
                var found = parent.GetComponentsInChildren<Card>(true);
                foreach (var c in found)
                {
                    if (c == null) continue;
                    if (!PassesFilter(c.gameObject)) continue;
                    cards.Add(c);
                }
            }
            else
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child == null) continue;

                    if (!PassesFilter(child.gameObject)) continue;

                    var c = child.GetComponentInParent<Card>();
                    if (c != null) cards.Add(c);
                }
            }
        }

        // 2) Devolver al mazo como hace CardCollector
        int returned = 0;
        foreach (var card in cards)
        {
            if (card == null) continue;

            // Sacarla de cualquier zona antes de devolverla
            if (card.currentZone != null)
                card.currentZone.RemoveCard(card);

            deck.ReturnToBottom(card);
            returned++;
        }

        Debug.Log($"[TableCardReturner] Cartas devueltas al mazo: {returned}");
    }

    private bool PassesFilter(GameObject go)
    {
        if (string.IsNullOrEmpty(cardTag)) return true;
        return go.CompareTag(cardTag);
    }
}
