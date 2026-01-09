using UnityEngine;
using System.Collections.Generic;

public class RandomCard : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Slots de preview (3)")]
    public Transform[] previewSlots;

    [Header("Destino de la carta elegida")]
    public Transform selectedCardSlot;

    [Header("Menú que se cerrará")]
    public GameObject menuRoot;

    private List<GameObject> previewedCards = new List<GameObject>();

    // =============================
    // MOSTRAR 3 CARTAS ALEATORIAS
    // =============================
    public void ShowRandomThree()
    {
        ClearPreview();

        if (deck == null || previewSlots == null || previewSlots.Length < 3)
        {
            Debug.LogWarning("RandomCard: configuración incompleta.");
            return;
        }

        // Acceso al runtimeDeck real
        var runtimeDeckField = typeof(DeckXR).GetField(
            "runtimeDeck",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (runtimeDeckField == null)
        {
            Debug.LogError("RandomCard: no se pudo acceder a runtimeDeck.");
            return;
        }

        List<GameObject> runtimeDeck =
            runtimeDeckField.GetValue(deck) as List<GameObject>;

        if (runtimeDeck == null || runtimeDeck.Count < 3)
        {
            Debug.LogWarning("RandomCard: no hay suficientes cartas.");
            return;
        }

        // Copia temporal para evitar repetidas
        List<GameObject> tempDeck = new List<GameObject>(runtimeDeck);

        Debug.Log("=== Cartas aleatorias ===");

        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, tempDeck.Count);
            GameObject prefab = tempDeck[randomIndex];
            tempDeck.RemoveAt(randomIndex);

            Card cardData = prefab.GetComponent<Card>() ?? prefab.GetComponentInChildren<Card>();
            if (cardData != null)
                Debug.Log($"{cardData.rank} of {cardData.suit}");

            GameObject cardObj = Instantiate(
                prefab,
                previewSlots[i].position,
                previewSlots[i].rotation
            );

            // Asegurar collider
            if (cardObj.GetComponent<Collider>() == null)
                cardObj.AddComponent<BoxCollider>();

            // Hacerla seleccionable
            SelectablePreviewCard selectable =
                cardObj.AddComponent<SelectablePreviewCard>();
            selectable.Init(this);

            // Configurar Card
            Card instanceCard = cardObj.GetComponent<Card>() ?? cardObj.GetComponentInChildren<Card>();
            if (instanceCard != null)
            {
                instanceCard.deck = deck;
                instanceCard.prefabReference = prefab;

                if (deck.jokerMaterial != null)
                {
                    instanceCard.jokerMaterial = deck.jokerMaterial;
                    instanceCard.SetHidden(false);
                }
            }

            previewedCards.Add(cardObj);
        }
    }

    // =============================
    // CUANDO SE SELECCIONA UNA CARTA
    // =============================
    public void OnCardSelected(GameObject selectedCard)
    {
        Debug.Log("Carta seleccionada");

        // Mover carta al slot final
        selectedCard.transform.position = selectedCardSlot.position;
        selectedCard.transform.rotation = selectedCardSlot.rotation;

        // ❌ Aquí está el problema: destruía las otras cartas mientras XRGrabInteractable estaba activo
        foreach (var card in previewedCards)
        {
            if (card != selectedCard && card != null)
                Destroy(card);
        }

        // Limpiar lista y dejar solo la carta seleccionada
        previewedCards.Clear();
        previewedCards.Add(selectedCard);

        // Cerrar menú
        if (menuRoot != null)
            menuRoot.SetActive(false);

        // Activar interacción con ratón
        if (selectedCard.GetComponent<MouseGrabXRProxy>() == null)
            selectedCard.AddComponent<MouseGrabXRProxy>();

        // Activar XRGrabInteractable
        var grab = selectedCard.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
            grab.enabled = true;
    }


    // =============================
    // LIMPIEZA
    // =============================
    public void ClearPreview()
    {
        foreach (var card in previewedCards)
        {
            if (card != null)
                Destroy(card);
        }

        previewedCards.Clear();
    }
}
