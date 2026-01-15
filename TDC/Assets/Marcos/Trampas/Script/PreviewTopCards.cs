using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PreviewTopCards : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Preview slots (3 por mano)")]
    public Transform[] previewSlotsRight; // 3
    public Transform[] previewSlotsLeft;  // 3

    [Header("Tiempo entre aparición de cartas")]
    public float delayBetweenCards = 0.5f;

    [Header("Animación de girar carta (opcional)")]
    public float flipDuration = 1f;

    [Header("Ángulo de giro (grados X/Y/Z)")]
    public float flipAngleX = 180f;
    public float flipAngleY = 180f;
    public float flipAngleZ = 180f;

    // Guardamos pares (derecha, izquierda)
    private readonly List<(GameObject right, GameObject left)> previewPairs = new();

    /// <summary>
    /// Muestra secuencialmente las siguientes 3 cartas del mazo (misma carta en ambas manos).
    /// </summary>
    public void ShowNextThreeSequential()
    {
        ClearPreview();

        if (deck == null ||
            previewSlotsRight == null || previewSlotsLeft == null ||
            previewSlotsRight.Length < 3 || previewSlotsLeft.Length < 3)
        {
            Debug.LogWarning("PreviewNextCards: falta configurar deck o previewSlotsRight/Left (3 por mano).");
            return;
        }

        var runtimeDeckField = typeof(DeckXR).GetField("runtimeDeck",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (runtimeDeckField == null)
        {
            Debug.LogError("PreviewNextCards: no se pudo acceder a runtimeDeck.");
            return;
        }

        List<GameObject> runtimeDeck = runtimeDeckField.GetValue(deck) as List<GameObject>;
        if (runtimeDeck == null || runtimeDeck.Count == 0)
        {
            Debug.LogWarning("PreviewNextCards: mazo vacío.");
            return;
        }

        int count = Mathf.Min(3, runtimeDeck.Count, previewSlotsRight.Length, previewSlotsLeft.Length);
        StartCoroutine(ShowCardsCoroutine(runtimeDeck, count));
    }

    private IEnumerator ShowCardsCoroutine(List<GameObject> deckList, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = deckList[i];

            // ✅ HIJOS + localPosition(0,0,0)
            GameObject cardRight = Instantiate(prefab, previewSlotsRight[i]);
            ResetLocal(cardRight);

            GameObject cardLeft = Instantiate(prefab, previewSlotsLeft[i]);
            ResetLocal(cardLeft);

            DisablePreviewInteraction(cardRight);
            DisablePreviewInteraction(cardLeft);

            SetupCardInstance(cardRight, prefab);
            SetupCardInstance(cardLeft, prefab);

            previewPairs.Add((cardRight, cardLeft));

            if (flipDuration > 0f)
                yield return StartCoroutine(FlipPairLocal(cardRight, cardLeft));

            yield return new WaitForSeconds(delayBetweenCards);
        }
    }

    private void ResetLocal(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.localPosition = Vector3.zero;        // ✅ (0,0,0)
        obj.transform.localRotation = Quaternion.identity; // ✅ (0,0,0)
        obj.transform.localScale = Vector3.one;
    }

    private void SetupCardInstance(GameObject cardObj, GameObject prefab)
    {
        if (cardObj == null) return;

        Card instanceCard = cardObj.GetComponent<Card>() ?? cardObj.GetComponentInChildren<Card>();
        if (instanceCard != null)
        {
            instanceCard.deck = deck;
            instanceCard.prefabReference = prefab;

            if (deck != null && deck.jokerMaterial != null)
            {
                instanceCard.jokerMaterial = deck.jokerMaterial;
                instanceCard.SetHidden(false);
            }
        }
    }

    private void DisablePreviewInteraction(GameObject cardObj)
    {
        if (cardObj == null) return;

        var grabs = cardObj.GetComponentsInChildren<XRGrabInteractable>(true);
        for (int i = 0; i < grabs.Length; i++)
            grabs[i].enabled = false;

        var colliders = cardObj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        var rbs = cardObj.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            rbs[i].isKinematic = true;
            rbs[i].detectCollisions = false;
            rbs[i].useGravity = false;
        }
    }

    // ✅ Flip en LOCAL (mejor al ser hijos)
    private IEnumerator FlipPairLocal(GameObject right, GameObject left)
    {
        float time = 0f;

        Quaternion startR = right ? right.transform.localRotation : Quaternion.identity;
        Quaternion startL = left ? left.transform.localRotation : Quaternion.identity;

        Quaternion delta = Quaternion.Euler(flipAngleX, flipAngleY, flipAngleZ);
        Quaternion endR = startR * delta;
        Quaternion endL = startL * delta;

        while (time < flipDuration)
        {
            float t = time / flipDuration;

            if (right) right.transform.localRotation = Quaternion.Slerp(startR, endR, t);
            if (left)  left.transform.localRotation  = Quaternion.Slerp(startL, endL, t);

            time += Time.deltaTime;
            yield return null;
        }

        if (right) right.transform.localRotation = endR;
        if (left)  left.transform.localRotation  = endL;
    }

    public void ClearPreview()
    {
        StopAllCoroutines();

        for (int i = 0; i < previewPairs.Count; i++)
        {
            var pair = previewPairs[i];
            if (pair.right != null) Destroy(pair.right);
            if (pair.left != null) Destroy(pair.left);
        }

        previewPairs.Clear();
    }
}
