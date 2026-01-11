using UnityEngine;
using System.Collections.Generic;

public class CardCollector : MonoBehaviour
{
    [Header("Collider ESFERA que usamos para recoger")]
    public Collider collectCollider;

    [Header("Mazo al que mandamos las cartas")]
    public DeckXR deck;

    [Header("Permitir que este jugador recoja cartas")]
    public bool canCollectCards = true;

    //  OPTIMIZACIÓN: Cachear componentes Card
    private Dictionary<Collider, Card> colliderToCardCache = new Dictionary<Collider, Card>();

    //  OPTIMIZACIÓN: Cooldown para evitar procesar la misma carta múltiples veces
    private HashSet<Card> processedCards = new HashSet<Card>();
    private float lastClearTime = 0f;
    private const float CLEAR_INTERVAL = 0.5f;

    private void Start()
    {
        if (collectCollider == null)
            collectCollider = GetComponent<Collider>();

        if (collectCollider != null)
            collectCollider.enabled = false;
    }

    //  OPTIMIZACIÓN: Limpiar periódicamente el set de cartas procesadas
    private void Update()
    {
        if (Time.time - lastClearTime > CLEAR_INTERVAL)
        {
            processedCards.Clear();
            lastClearTime = Time.time;
        }
    }

    public void StartCollecting()
    {
        if (!canCollectCards) return;

        if (collectCollider != null)
        {
            collectCollider.enabled = true;
            processedCards.Clear(); // Limpiar al empezar a recoger
        }
    }

    public void StopCollecting()
    {
        if (collectCollider != null)
        {
            collectCollider.enabled = false;
            processedCards.Clear(); // Limpiar al dejar de recoger
            colliderToCardCache.Clear(); // Limpiar caché
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //  OPTIMIZACIÓN: Cachear en OnTriggerEnter
        if (!colliderToCardCache.ContainsKey(other))
        {
            Card card = other.GetComponentInParent<Card>();
            if (card != null)
            {
                colliderToCardCache[other] = card;
            }
        }

        TryCollect(other);
    }

    //  OPTIMIZACIÓN: OnTriggerStay es muy costoso, considerar eliminarlo
    // Si necesitas que funcione mientras mantienes el grip, usa un timer en Update
    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // Limpiar caché cuando sale
        if (colliderToCardCache.TryGetValue(other, out Card card))
        {
            colliderToCardCache.Remove(other);
            processedCards.Remove(card);
        }
    }

    private void TryCollect(Collider other)
    {
        if (!canCollectCards) return;

        if (collectCollider == null || !collectCollider.enabled)
            return;

        //  OPTIMIZACIÓN: Usar caché en lugar de GetComponentInParent
        if (!colliderToCardCache.TryGetValue(other, out Card card))
            return;

        if (card == null) return;

        //  OPTIMIZACIÓN: Evitar procesar la misma carta múltiples veces
        if (processedCards.Contains(card))
            return;

        processedCards.Add(card);

        // Sacarla de cualquier zona antes de devolverla
        if (card.currentZone != null)
            card.currentZone.RemoveCard(card);

        deck.ReturnToBottom(card);
    }

    public void SetCanCollectCards(bool value)
    {
        canCollectCards = value;

        if (!value)
            StopCollecting();
    }

    // Codigo de Emilio, necesario para conexion con los managers
    public void SetCanCollect(bool canCollect)
    {
        canCollectCards = canCollect;
    }

}