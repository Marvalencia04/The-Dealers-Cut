using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DeckXR : MonoBehaviour
{
    [Header("Cartas")]
    [Tooltip("Prefabs de UNA baraja completa")]
    public List<GameObject> singleDeckPrefabs;

    [Tooltip("Cuántas barajas quieres en el mazo")]
    public int numberOfDecks = 7;

    [Header("Spawn")]
    public Transform spawnPoint;

    [Header("XR")]
    public XRInteractionManager interactionManager;

    private List<GameObject> runtimeDeck = new List<GameObject>();

    private void Awake()
    {
        BuildDeck();
        Shuffle();
    }

    private void BuildDeck()
    {
        runtimeDeck.Clear();

        if (singleDeckPrefabs == null || singleDeckPrefabs.Count == 0)
            return;

        for (int d = 0; d < numberOfDecks; d++)
        {
            foreach (var card in singleDeckPrefabs)
            {
                runtimeDeck.Add(card);
            }
        }
    }

    public void Shuffle()
    {
        for (int i = 0; i < runtimeDeck.Count; i++)
        {
            int j = Random.Range(i, runtimeDeck.Count);
            (runtimeDeck[i], runtimeDeck[j]) = (runtimeDeck[j], runtimeDeck[i]);
        }
    }

    // Evento al pulsar trigger sobre el mazo
    public void DrawFromDeck(SelectEnterEventArgs args)
    {
        if (runtimeDeck.Count == 0) return;

        var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
        if (interactor == null)
        {
            Debug.LogWarning("DrawFromDeck: interactor nulo");
            return;
        }

        GameObject prefab = runtimeDeck[0];
        runtimeDeck.RemoveAt(0);

        // Intentamos usar la transform del interactor (controlador)
        Transform interactorTransform = (interactor as MonoBehaviour)?.transform;

        Vector3 pos;
        Quaternion rot;

        if (interactorTransform != null)
        {
            pos = interactorTransform.position;
            rot = interactorTransform.rotation;
        }
        else if (spawnPoint != null)
        {
            pos = spawnPoint.position;
            rot = spawnPoint.rotation;
        }
        else
        {
            pos = transform.position;
            rot = transform.rotation;
        }

        GameObject cardObj = Instantiate(prefab, pos, rot);

        Card cardComp = cardObj.GetComponent<Card>();
        if (cardComp != null)
        {
            cardComp.deck = this;
            cardComp.prefabReference = prefab;
        }

        // Que se pueda coger con XR
        UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab = cardObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
            grab = cardObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (interactionManager != null)
        {
            interactionManager.SelectEnter(interactor, grab);
        }
    }


    public void ReturnToBottom(Card card)
    {
        if (card == null) return;

        if (card.currentZone != null)
            card.currentZone.RemoveCard(card);

        if (card.prefabReference != null)
            runtimeDeck.Add(card.prefabReference);

        Destroy(card.gameObject);
    }

    public void RebuildAndShuffle()
    {
        BuildDeck();
        Shuffle();
    }
}
