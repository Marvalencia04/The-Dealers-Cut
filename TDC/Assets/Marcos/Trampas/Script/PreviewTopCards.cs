using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PreviewNextCards : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Transform donde colocar cada carta")]
    public Transform[] previewSlots; // Deben ser 3

    [Header("Tiempo entre aparición de cartas")]
    public float delayBetweenCards = 0.5f;

    [Header("Animación de girar carta (opcional)")]
    public float flipDuration = 1f; // duración del giro

    [Header("Ángulo de giro de la carta (grados X)")]
    public float flipAngleX = 180f;
    [Header("Ángulo de giro de la carta (grados Y)")]
    public float flipAngleY = 180f; 
    [Header("Ángulo de giro de la carta (grados Z)")]
    public float flipAngleZ = 180f; 

    private List<GameObject> previewedCards = new List<GameObject>();

    /// <summary>
    /// Activa la aparición secuencial de las siguientes 3 cartas.
    /// </summary>
    public void ShowNextThreeSequential()
    {
        ClearPreview();

        if (deck == null || previewSlots.Length < 3)
        {
            Debug.LogWarning("Falta configurar deck o previewSlots.");
            return;
        }

        var runtimeDeckField = typeof(DeckXR).GetField("runtimeDeck",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
        StartCoroutine(ShowCardsCoroutine(runtimeDeck, count));
    }

    /// <summary>
    /// Coroutine que instancia cartas una tras otra con delay.
    /// </summary>
    private IEnumerator ShowCardsCoroutine(List<GameObject> deckList, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = deckList[i];

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

            // Animación de girar la carta
            if (flipDuration > 0f)
            {
                yield return StartCoroutine(FlipCard(cardObj));
            }

            // Espera antes de mostrar la siguiente carta
            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    /// <summary>
    /// Gira la carta en Y según el ángulo configurado.
    /// </summary>
    private IEnumerator FlipCard(GameObject card)
    {
        float time = 0f;
        if (card == null) yield break; // <-- evitar errores si ya fue destruida

        Quaternion startRot = card.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(flipAngleX, flipAngleY, flipAngleZ);

        while (time < flipDuration)
        {
            if (card == null) yield break; // <-- chequeo en cada frame
            card.transform.rotation = Quaternion.Slerp(startRot, endRot, time / flipDuration);
            time += Time.deltaTime;
            yield return null;
        }

        if (card != null)
            card.transform.rotation = endRot;
    }


    /// <summary>
    /// Borra todas las cartas actuales.
    /// </summary>
    public void ClearPreview()
    {
        StopAllCoroutines(); // Detener animaciones activas

        foreach (var c in previewedCards)
            if (c != null)
                Destroy(c);

        previewedCards.Clear();
    }

}
