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

    //  OPTIMIZACI�N: Cachear componentes Card
    private Dictionary<Collider, Card> colliderToCardCache = new Dictionary<Collider, Card>();

    //  OPTIMIZACI�N: Cooldown para evitar procesar la misma carta m�ltiples veces
    private HashSet<Card> processedCards = new HashSet<Card>();
    private float lastClearTime = 0f;
    private const float CLEAR_INTERVAL = 0.5f;

    [Header("FX - Recoger cartas")]
    [SerializeField] private AudioSource fxSource;

    [SerializeField] private List<AudioClip> collectFX = new();

    [Range(0f, 1f)]
    [SerializeField] private float fxVolume = 1f;

    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);


    private void Start()
    {
        if (collectCollider == null)
            collectCollider = GetComponent<Collider>();

        if (collectCollider != null)
            collectCollider.enabled = false;
    }

    //  OPTIMIZACI�N: Limpiar peri�dicamente el set de cartas procesadas
    private void Update()
    {
        if (Time.time - lastClearTime > CLEAR_INTERVAL)
        {
            processedCards.Clear();
            lastClearTime = Time.time;
        }
    }

    private void PlayCollectFX()
    {
        if (fxSource == null || collectFX == null || collectFX.Count == 0) return;

        int index = Random.Range(0, collectFX.Count);

        float oldPitch = fxSource.pitch;
        fxSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        fxSource.PlayOneShot(collectFX[index], fxVolume);
        fxSource.pitch = oldPitch;
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
            colliderToCardCache.Clear(); // Limpiar cach�
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //  OPTIMIZACI�N: Cachear en OnTriggerEnter
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

    //  OPTIMIZACI�N: OnTriggerStay es muy costoso, considerar eliminarlo
    // Si necesitas que funcione mientras mantienes el grip, usa un timer en Update
    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerExit(Collider other)
    {
        // Limpiar cach� cuando sale
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

        //  OPTIMIZACI�N: Usar cach� en lugar de GetComponentInParent
        if (!colliderToCardCache.TryGetValue(other, out Card card))
            return;

        if (card == null) return;

        //  OPTIMIZACI�N: Evitar procesar la misma carta m�ltiples veces
        if (processedCards.Contains(card))
            return;

        processedCards.Add(card);

        // Sacarla de cualquier zona antes de devolverla
        if (card.currentZone != null)
            card.currentZone.RemoveCard(card);
        PlayCollectFX();

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