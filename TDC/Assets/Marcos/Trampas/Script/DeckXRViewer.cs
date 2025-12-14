using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DeckXRViewer : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Contenedores donde aparecerán las cartas (Canvas World Space)")]
    public Transform slot1;
    public Transform slot2;
    public Transform slot3;

    [Header("Escala que tendrán dentro del Canvas")]
    public float cardScale = 0.2f;

    [Header("Desactivar XRGrabInteractable en las copias del preview?")]
    public bool disableGrab = true;

    // Para destruir cartas anteriores
    private GameObject inst1;
    private GameObject inst2;
    private GameObject inst3;

    public void MostrarSiguientesTres()
    {
        if (deck == null)
        {
            Debug.LogError("[DeckXRViewer] No hay DeckXR asignado.");
            return;
        }

        List<GameObject> deckList = ObtenerDeckInterno(deck);

        if (deckList == null)
        {
            Debug.LogError("[DeckXRViewer] No se pudo leer runtimeDeck.");
            return;
        }

        Debug.Log($"[DeckXRViewer] === MOSTRANDO SIGUIENTES 3 CARTAS ===");
        Debug.Log($"[DeckXRViewer] Cartas restantes en mazo: {deckList.Count}");

        MostrarCarta(0, ref inst1, slot1, deckList);
        MostrarCarta(1, ref inst2, slot2, deckList);
        MostrarCarta(2, ref inst3, slot3, deckList);
    }

    private void MostrarCarta(int index, ref GameObject currentInst, Transform slot, List<GameObject> deckList)
    {
        if (slot == null) return;

        // Eliminar la carta anterior del slot
        if (currentInst != null)
            Destroy(currentInst);

        if (index >= deckList.Count)
        {
            Debug.Log($"[DeckXRViewer] Carta {index + 1}: (NO HAY CARTA)");
            return;
        }

        GameObject prefab = deckList[index];
        Card cardData = prefab.GetComponent<Card>();

        if (cardData == null)
        {
            Debug.Log($"[DeckXRViewer] Carta {index + 1}: El prefab NO tiene Card.cs");
            return;
        }

        // 🔥 LOG IMPORTANTE
        Debug.Log($"[DeckXRViewer] Carta {index + 1}: {cardData.rank} of {cardData.suit}");

        // Instancia la carta dentro del canvas
        currentInst = Instantiate(prefab, slot.position, slot.rotation, slot);

        // Escala
        currentInst.transform.localScale = Vector3.one * cardScale;

        // Limpiar enlaces para evitar que sea una carta jugable
        Card newCard = currentInst.GetComponent<Card>();
        if (newCard != null)
        {
            newCard.deck = null; // no pertenece al mazo
            newCard.SetHidden(false); // mostrar materiales reales
        }

        // Desactivar XRGrabInteractable si corresponde
        if (disableGrab)
        {
            var grab = currentInst.GetComponent<XRGrabInteractable>();
            if (grab != null)
                Destroy(grab);
        }
    }

    // Acceso al runtimeDeck por reflexión
    private List<GameObject> ObtenerDeckInterno(DeckXR deck)
    {
        var field = typeof(DeckXR).GetField("runtimeDeck",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        return field?.GetValue(deck) as List<GameObject>;
    }
}
