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

    [Header("Control del mazo")]
    [Tooltip("Si es false, NO se pueden sacar cartas del mazo.")]
    public bool canDrawCards = true;

    [Header("Visual")]
    [Tooltip("Material que se usa para ocultar las cartas (Joker).")]
    public Material jokerMaterial;

    [Header("FX - Sonido")]
    [Tooltip("AudioSource para efectos (NO música)")]
    [SerializeField] private AudioSource fxSource;

    [Tooltip("Sonidos al sacar una carta del mazo")]
    [SerializeField] private List<AudioClip> drawCardFX = new();

    [Range(0f, 1f)]
    [SerializeField] private float fxVolume = 1f;



    private List<GameObject> runtimeDeck = new List<GameObject>();

    private void Awake()
    {
        BuildDeck();
        Shuffle();
    }

    // ===============================
    // CONSTRUCCIÓN Y BARAJADO
    // ===============================

    private void BuildDeck()
    {
        runtimeDeck.Clear();

        if (singleDeckPrefabs == null || singleDeckPrefabs.Count == 0)
            return;

        for (int d = 0; d < numberOfDecks; d++)
        {
            foreach (var card in singleDeckPrefabs)
                runtimeDeck.Add(card);
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

    // ===============================
    // SACAR CARTAS
    // ===============================

    public void DrawFromDeck(SelectEnterEventArgs args)
    {
        // ⛔ No dejar sacar cartas si está bloqueado
        if (!canDrawCards)
        {
            Debug.Log("DeckXR: no se pueden sacar cartas ahora.");
            return;
        }

        if (runtimeDeck.Count == 0) return;

        var interactor = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor;
        if (interactor == null)
        {
            Debug.LogWarning("DrawFromDeck: interactor nulo");
            return;
        }

        GameObject prefab = runtimeDeck[0];
        runtimeDeck.RemoveAt(0);

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
        PlayDrawFX();
        
        Card cardComp = cardObj.GetComponent<Card>();
        if (cardComp != null)
        {
            cardComp.deck = this;
            cardComp.prefabReference = prefab;

            // ⬇⬇ NUEVO: inicializar visual en modo oculto
            if (jokerMaterial != null)
            {
                cardComp.jokerMaterial = jokerMaterial;
                cardComp.SetHidden(true);   // la carta sale con textura Joker
            }
        }


        // Asegurar XRGrabInteractable
        var grab = cardObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab == null)
            grab = cardObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (interactionManager != null)
            interactionManager.SelectEnter(interactor, grab);
    }

    private void PlayDrawFX()
    {
        if (fxSource == null || drawCardFX == null || drawCardFX.Count == 0)
            return;

        int index = Random.Range(0, drawCardFX.Count);
        fxSource.PlayOneShot(drawCardFX[index], fxVolume);
    }


    // ===============================
    // DEVOLVER CARTAS
    // ===============================

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

    // ===============================
    // MÉTODOS PARA EL GAME MANAGER
    // ===============================

    public void SetCanDrawCards(bool value)
    {
        canDrawCards = value;
    }
}
