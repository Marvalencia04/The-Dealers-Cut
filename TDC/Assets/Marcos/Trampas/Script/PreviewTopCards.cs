using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PreviewNextCards : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Transform donde colocar cada carta")]
    public Transform[] previewSlots; // Deben ser 3

    private List<GameObject> previewedCards = new List<GameObject>();

    private void Start()
    {
        // Esperar un frame para asegurar que DeckXR haya hecho Awake() y Shuffle()
        //StartCoroutine(DelayedShow());
    }

    private IEnumerator DelayedShow()
    {
        yield return null; // espera un frame
        ShowNextThree();
    }

    public void ShowNextThree()
    {
        ClearPreview();

        if (deck == null || previewSlots.Length < 3)
        {
            Debug.LogWarning("Falta configurar deck o previewSlots.");
            return;
        }

        // Acceder a runtimeDeck mediante reflexión
        var runtimeDeckField = typeof(DeckXR).GetField("runtimeDeck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (runtimeDeckField == null)
        {
            Debug.LogError("No se pudo acceder a runtimeDeck");
            return;
        }

        List<GameObject> runtimeDeck = runtimeDeckField.GetValue(deck) as List<GameObject>;
        if (runtimeDeck == null || runtimeDeck.Count == 0)
        {
            Debug.LogWarning("Mazo vacío");
            return;
        }

        int count = Mathf.Min(3, runtimeDeck.Count, previewSlots.Length);

        Debug.Log("=== Siguientes cartas en el mazo ===");

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = runtimeDeck[i];

            // Intentar obtener Card en raíz o en hijos
            Card cardComp = prefab.GetComponent<Card>() ?? prefab.GetComponentInChildren<Card>();

            if (cardComp != null)
                Debug.Log($"Carta {i + 1}: {cardComp.rank} of {cardComp.suit}");
            else
                Debug.Log($"Carta {i + 1}: Prefab sin componente Card");

            // Instanciar la carta en preview
            GameObject cardObj = Instantiate(prefab, previewSlots[i].position, previewSlots[i].rotation);

            // Configurar visual
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

    public void ClearPreview()
    {
        foreach (var c in previewedCards)
            if (c != null)
                Destroy(c);

        previewedCards.Clear();
    }
}
