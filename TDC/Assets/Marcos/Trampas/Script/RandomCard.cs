using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RandomCard : MonoBehaviour
{
    [Header("Referencia al mazo")]
    public DeckXR deck;

    [Header("Slots de preview (3 por mano)")]
    public Transform[] previewSlotsRight; // 3
    public Transform[] previewSlotsLeft;  // 3

    [Header("Destino de la carta elegida (opcional, por mano)")]
    public Transform selectedCardSlotRight;
    public Transform selectedCardSlotLeft;

    [Header("Menú que se cerrará")]
    public GameObject menuRoot;

    [Header("Animación de giro (preview)")]
    public float flipDuration = 1f;
    public float flipAngleX = 180f;
    public float flipAngleY = 180f;
    public float flipAngleZ = 180f;

    // Pares (derecha, izquierda)
    private readonly List<(GameObject right, GameObject left)> previewPairs = new();

    // =============================
    // MOSTRAR 3 CARTAS ALEATORIAS
    // =============================
    public void ShowRandomThree()
    {
        ClearPreview();

        if (deck == null ||
            previewSlotsRight == null || previewSlotsLeft == null ||
            previewSlotsRight.Length < 3 || previewSlotsLeft.Length < 3)
        {
            Debug.LogWarning("RandomCard: configuración incompleta (slots derecha/izquierda).");
            return;
        }

        var runtimeDeckField = typeof(DeckXR).GetField(
            "runtimeDeck",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (runtimeDeckField == null)
        {
            Debug.LogError("RandomCard: no se pudo acceder a runtimeDeck.");
            return;
        }

        List<GameObject> runtimeDeck = runtimeDeckField.GetValue(deck) as List<GameObject>;
        if (runtimeDeck == null || runtimeDeck.Count < 3)
        {
            Debug.LogWarning("RandomCard: no hay suficientes cartas.");
            return;
        }

        // Copia temporal para evitar repetidas
        List<GameObject> tempDeck = new List<GameObject>(runtimeDeck);

        Debug.Log("=== Cartas aleatorias (doble mano) ===");

        // Elegimos 3 prefabs y los instanciamos en ambas manos
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = Random.Range(0, tempDeck.Count);
            GameObject prefab = tempDeck[randomIndex];
            tempDeck.RemoveAt(randomIndex);

            Card cardData = prefab.GetComponent<Card>() ?? prefab.GetComponentInChildren<Card>();
            if (cardData != null)
                Debug.Log($"{cardData.rank} of {cardData.suit}");

            GameObject rightObj = SpawnPreviewCard(prefab, previewSlotsRight[i]);
            GameObject leftObj  = SpawnPreviewCard(prefab, previewSlotsLeft[i]);

            // Hacerlas seleccionables:
            // Si clicas cualquiera, se selecciona el par.
            var selectableRight = rightObj.AddComponent<SelectablePreviewCard>();
            selectableRight.Init(this);
            var linkR = rightObj.AddComponent<PreviewPairLink>();
            linkR.other = leftObj;

            var selectableLeft = leftObj.AddComponent<SelectablePreviewCard>();
            selectableLeft.Init(this);
            var linkL = leftObj.AddComponent<PreviewPairLink>();
            linkL.other = rightObj;

            previewPairs.Add((rightObj, leftObj));
        }

        // ✅ Giro animado para TODAS las cartas (sin coroutines, versión simple con Invoke)
        // Si prefieres que el giro sea secuencial (1, luego 2, luego 3), te lo hago también.
        if (flipDuration > 0f)
        {
            // Arrancamos una coroutine desde aquí de forma segura
            StartCoroutine(FlipAllPairsLocal());
        }
    }

    private System.Collections.IEnumerator FlipAllPairsLocal()
    {
        // Giramos todas a la vez
        float time = 0f;

        // Guardamos starts/ends
        var startsR = new Quaternion[previewPairs.Count];
        var startsL = new Quaternion[previewPairs.Count];
        var endsR = new Quaternion[previewPairs.Count];
        var endsL = new Quaternion[previewPairs.Count];

        Quaternion delta = Quaternion.Euler(flipAngleX, flipAngleY, flipAngleZ);

        for (int i = 0; i < previewPairs.Count; i++)
        {
            var (r, l) = previewPairs[i];
            startsR[i] = r ? r.transform.localRotation : Quaternion.identity;
            startsL[i] = l ? l.transform.localRotation : Quaternion.identity;
            endsR[i] = startsR[i] * delta;
            endsL[i] = startsL[i] * delta;
        }

        while (time < flipDuration)
        {
            float t = time / flipDuration;

            for (int i = 0; i < previewPairs.Count; i++)
            {
                var (r, l) = previewPairs[i];
                if (r) r.transform.localRotation = Quaternion.Slerp(startsR[i], endsR[i], t);
                if (l) l.transform.localRotation = Quaternion.Slerp(startsL[i], endsL[i], t);
            }

            time += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < previewPairs.Count; i++)
        {
            var (r, l) = previewPairs[i];
            if (r) r.transform.localRotation = endsR[i];
            if (l) l.transform.localRotation = endsL[i];
        }
    }

    // =============================
    // CUANDO SE SELECCIONA UNA CARTA
    // =============================
    public void OnCardSelected(GameObject clickedCard)
    {
        if (clickedCard == null) return;

        Debug.Log("Carta seleccionada (doble mano)");

        // Identificar el par seleccionado
        GameObject selectedRight = null;
        GameObject selectedLeft = null;

        for (int i = 0; i < previewPairs.Count; i++)
        {
            var pair = previewPairs[i];
            if (pair.right == clickedCard || pair.left == clickedCard)
            {
                selectedRight = pair.right;
                selectedLeft = pair.left;
                break;
            }
        }

        // Mover al slot final (si existe). Si no, se quedan donde están.
        if (selectedCardSlotRight != null && selectedRight != null)
        {
            selectedRight.transform.SetParent(selectedCardSlotRight, false);
            ResetLocal(selectedRight);
        }

        if (selectedCardSlotLeft != null && selectedLeft != null)
        {
            selectedLeft.transform.SetParent(selectedCardSlotLeft, false);
            ResetLocal(selectedLeft);
        }

        // Destruir los otros pares
        for (int i = 0; i < previewPairs.Count; i++)
        {
            var pair = previewPairs[i];
            bool isSelectedPair = (pair.right == selectedRight) || (pair.left == selectedLeft);

            if (!isSelectedPair)
            {
                if (pair.right != null) Destroy(pair.right);
                if (pair.left != null) Destroy(pair.left);
            }
        }

        previewPairs.Clear();
        previewPairs.Add((selectedRight, selectedLeft));

        // Cerrar menú
        if (menuRoot != null)
            menuRoot.SetActive(false);

        // Activar interacción (si quieres SOLO una mano, dime y lo ajusto)
        EnableInteraction(selectedRight);
        EnableInteraction(selectedLeft);
    }

    // =============================
    // SPAWN: hijo + localPosition(0,0,0) + config Card
    // =============================
    private GameObject SpawnPreviewCard(GameObject prefab, Transform slot)
    {
        GameObject cardObj = Instantiate(prefab, slot); // ✅ hijo del slot
        ResetLocal(cardObj); // ✅ pos 0,0,0

        // Asegurar collider si no hay ninguno en root
        if (cardObj.GetComponent<Collider>() == null)
            cardObj.AddComponent<BoxCollider>();

        // Configurar Card
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

        // Preview: desactivar grab si existe
        var grab = cardObj.GetComponent<XRGrabInteractable>();
        if (grab != null) grab.enabled = false;

        return cardObj;
    }

    private void ResetLocal(GameObject obj)
    {
        if (obj == null) return;
        obj.transform.localPosition = Vector3.zero;        // ✅ (0,0,0)
        obj.transform.localRotation = Quaternion.identity; // ✅ (0,0,0)
        obj.transform.localScale = Vector3.one;
    }

    private void EnableInteraction(GameObject cardObj)
    {
        if (cardObj == null) return;

        if (cardObj.GetComponent<MouseGrabXRProxy>() == null)
            cardObj.AddComponent<MouseGrabXRProxy>();

        var grab = cardObj.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.enabled = true;
    }

    // =============================
    // LIMPIEZA
    // =============================
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

/// <summary>
/// Enlace mínimo para tener referencia al “gemelo” (por si lo necesitas).
/// </summary>
public class PreviewPairLink : MonoBehaviour
{
    public GameObject other;
}
