using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

// IMPORTANTE: para poder referenciar XRGrabInteractable aquí
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

        if (deck == null || previewSlots == null || previewSlots.Length < 3)
        {
            Debug.LogWarning("Falta configurar deck o previewSlots.");
            return;
        }

        // Acceder al runtimeDeck (privado) por reflection
        var runtimeDeckField = typeof(DeckXR).GetField("runtimeDeck",
            BindingFlags.NonPublic | BindingFlags.Instance);

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

            // Crea las cartas como hijos de los gameobjects
            GameObject cardObj = Instantiate(prefab, previewSlots[i]);

            cardObj.transform.localPosition = Vector3.zero;
            cardObj.transform.localRotation = Quaternion.identity;
            cardObj.transform.localScale = Vector3.one;


            // >>> PREVIEW: desactivar interacción (XR + colliders + físicas)
            DisablePreviewInteraction(cardObj);

            // Configurar visual / referencias de Card
            Card instanceCard = cardObj.GetComponent<Card>() ?? cardObj.GetComponentInChildren<Card>();
            if (instanceCard != null)
            {
                instanceCard.deck = deck;
                instanceCard.prefabReference = prefab;

                if (deck.jokerMaterial != null)
                {
                    instanceCard.jokerMaterial = deck.jokerMaterial;

                    // En tu lógica: SetHidden(false) = mostrar / no ocultar
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
    /// Desactiva toda interacción para que la carta sea solo "preview".
    /// </summary>
    private void DisablePreviewInteraction(GameObject cardObj)
    {
        if (cardObj == null) return;

        // 1) Desactivar XRGrabInteractable (en raíz y en hijos)
        var grabs = cardObj.GetComponentsInChildren<XRGrabInteractable>(true);
        for (int i = 0; i < grabs.Length; i++)
            grabs[i].enabled = false;

        // 2) Desactivar colliders (evita raycasts/choques/trigger)
        var colliders = cardObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        // 3) Asegurar que no haga físicas
        var rbs = cardObj.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;
            rbs[i].detectCollisions = false;
            rbs[i].useGravity = false;
        }

        // (Opcional) Evita que el XR Ray Interactor lo "vea" aunque tuviera collider:
        // cardObj.layer = LayerMask.NameToLayer("Ignore Raycast");
        // foreach (Transform t in cardObj.GetComponentsInChildren<Transform>(true))
        //     t.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    /// <summary>
    /// Gira la carta según el ángulo configurado.
    /// </summary>
    private IEnumerator FlipCard(GameObject card)
    {
        float time = 0f;
        if (card == null) yield break;

        Quaternion startRot = card.transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(flipAngleX, flipAngleY, flipAngleZ);

        while (time < flipDuration)
        {
            if (card == null) yield break;
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
        StopAllCoroutines();

        foreach (var c in previewedCards)
            if (c != null)
                Destroy(c);

        previewedCards.Clear();
    }
}
