using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class CardSnapZone : MonoBehaviour
{
    [Header("Posiciones donde se snappean las cartas (en orden)")]
    public Transform[] slots;

    [Header("Visual de cada slot (mismo ORDEN que slots)")]
    public Renderer[] slotVisuals;

    [Header("Ajuste fino dentro del slot")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localRotationOffsetEuler = Vector3.zero;

    [Header("Snap")]
    [Tooltip("Distancia máxima al slot para autosnap mientras la carta está en la mano.")]
    public float snapDistance = 0.08f;

    [Tooltip("Tiempo mínimo desde que levantas la carta hasta que puede volver a snappear.")]
    public float pickupGrace = 0.25f;

    [Header("Lógica de juego")]
    [Tooltip("Si es false, esta zona NO acepta nuevas cartas (como si estuviera cerrada).")]
    public bool canReceiveNewCards = true;

    [Tooltip("Si es false, no se pueden coger las cartas que ya están en esta zona.")]
    public bool canGrabFromZone = true;

    //----------------------------------------------------------------------------
    // Cambios Emilio
    //----------------------------------------------------------------------------
    [Tooltip("Maximo numero de cartas permitidas en esta zona (independiente del numero de slots).")]
    [SerializeField] private int maxCards = 2;
    //----------------------------------------------------------------------------

    //----------------------------------------------------------------------------
    // Cambios Emilio
    //----------------------------------------------------------------------------
    public System.Action<CardSnapZone> OnZoneChanged;
    //----------------------------------------------------------------------------


    [Header("UI (puntuación)")]
    public TMP_Text valueText;
    public bool logDebug = false;

    private Card[] occupied;

    // 🔥 OPTIMIZACIÓN: Cachear componentes en diccionario
    private Dictionary<Card, XRGrabInteractable> cardGrabCache = new Dictionary<Card, XRGrabInteractable>();
    
    // 🔥 OPTIMIZACIÓN: Evitar GetComponentInParent cada frame
    private Dictionary<Collider, Card> colliderToCardCache = new Dictionary<Collider, Card>();

    // 🔥 OPTIMIZACIÓN: Cooldown para OnTriggerStay
    private Dictionary<Card, float> lastCheckTime = new Dictionary<Card, float>();
    private const float CHECK_INTERVAL = 0.05f; // Solo chequear cada 50ms

    private void Awake()
    {
        occupied = new Card[slots.Length];

        if (slotVisuals == null || slotVisuals.Length == 0)
        {
            slotVisuals = new Renderer[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slotVisuals[i] = slots[i].GetComponentInChildren<Renderer>(true);
            }
        }
        else if (slotVisuals.Length != slots.Length)
        {
            Debug.LogWarning($"[CardSnapZone:{name}] slotVisuals y slots tienen longitudes distintas.");
        }

        UpdateSlotVisuals();
        RecalculateScore();
    }

    // 🔥 OPTIMIZACIÓN: Usar OnTriggerEnter y eventos en lugar de OnTriggerStay
    private void OnTriggerEnter(Collider other)
    {
        // Cachear el componente Card para este collider
        if (!colliderToCardCache.ContainsKey(other))
        {
            Card card = other.GetComponentInParent<Card>();
            if (card != null)
            {
                colliderToCardCache[other] = card;
                
                // Cachear también el XRGrabInteractable
                if (!cardGrabCache.ContainsKey(card))
                {
                    XRGrabInteractable grab = card.GetComponentInParent<XRGrabInteractable>();
                    if (grab != null)
                    {
                        cardGrabCache[card] = grab;
                        
                        // Suscribirse a eventos de grab
                        grab.selectEntered.AddListener(OnCardGrabbed);
                        grab.selectExited.AddListener(OnCardReleased);
                    }
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 🔥 OPTIMIZACIÓN: Usar caché en lugar de GetComponentInParent
        if (!colliderToCardCache.TryGetValue(other, out Card card))
            return;

        // 🔥 OPTIMIZACIÓN: Throttling - solo chequear cada X tiempo
        float currentTime = Time.time;
        if (lastCheckTime.TryGetValue(card, out float lastTime))
        {
            if (currentTime - lastTime < CHECK_INTERVAL)
                return;
        }
        lastCheckTime[card] = currentTime;

        // 🔥 OPTIMIZACIÓN: Usar caché para XRGrabInteractable
        if (!cardGrabCache.TryGetValue(card, out XRGrabInteractable grab))
            return;

        bool isGrabbed = grab != null && grab.isSelected;

        // 1) Si la carta está en ESTA zona y ahora la están cogiendo → ya se maneja en OnCardGrabbed
        if (card.currentZone == this && isGrabbed)
            return;

        // 2) Si pertenece a otra zona distinta, no la tocamos
        if (card.currentZone != null && card.currentZone != this)
            return;

        /* Cambio Emilio
        // 3) Solo queremos autosnap si está en la mano
        if (!isGrabbed)
            return;
        */

        //----------------------------------------------------------------------------
        //Cambios Emilio: permitir snap al soltar la carta
        //----------------------------------------------------------------------------
        if (!isGrabbed)
        {
            // Si ya pertenece a otra zona, no tocar
            if (card.currentZone != null)
                return;

            // Esperar un pequeño margen tras soltar
            float dtRelease = currentTime - card.lastGrabTime;
            if (dtRelease < pickupGrace)
                return;
        }

        // Si la zona NO acepta nuevas cartas, no snappeamos
        if (!canReceiveNewCards)
            return;




        //----------------------------------------------------------------------------
        // Cambios Emilio: limite duro de cartas
        //----------------------------------------------------------------------------
        if (GetOccupiedCount() >= Mathf.Max(1, maxCards))
        {
            if (logDebug)
                Debug.Log($"[CardSnapZone:{name}] Limite maxCards alcanzado ({maxCards})");
            return;
        }
        //----------------------------------------------------------------------------

        // 4) Respeta margen de tiempo desde que la levantaste
        float dt = currentTime - card.lastGrabTime;
        if (dt < pickupGrace)
            return;

        // 5) Usamos SIEMPRE el primer slot libre
        int index = GetFirstFreeSlot();
        if (index == -1) return;

        Transform targetSlot = slots[index];
        if (targetSlot == null) return;

        float distance = Vector3.Distance(card.transform.position, targetSlot.position);
        if (distance > snapDistance)
            return;

        // 6) Autosnap
        if (grab.isSelected && grab.interactionManager != null)
        {
            var interactors = grab.interactorsSelecting.ToList();
            foreach (var it in interactors)
            {
                grab.interactionManager.SelectExit(it, grab);
            }
        }

        SnapCard(card, index);
    }

    // 🔥 NUEVO: Listener para cuando se coge una carta
    private void OnCardGrabbed(SelectEnterEventArgs args)
    {
        if (args.interactableObject is XRGrabInteractable grab)
        {
            Card card = grab.GetComponent<Card>();
            if (card != null && card.currentZone == this)
            {
                if (logDebug) Debug.Log($"[CardSnapZone:{name}] Levantan {card.name} de esta zona");

                InternalRemoveCard(card);
                card.currentZone = null;
                card.lastGrabTime = Time.time;
            }
        }
    }

    // 🔥 NUEVO: Listener para cuando se suelta una carta
    private void OnCardReleased(SelectExitEventArgs args)
    {
        // Podrías usar esto si necesitas lógica adicional al soltar
    }

    private void OnTriggerExit(Collider other)
    {
        // Limpiar caché cuando sale del trigger
        if (colliderToCardCache.TryGetValue(other, out Card card))
        {
            lastCheckTime.Remove(card);
        }
    }

    private void OnDestroy()
    {
        // Limpiar listeners
        foreach (var kvp in cardGrabCache)
        {
            if (kvp.Value != null)
            {
                kvp.Value.selectEntered.RemoveListener(OnCardGrabbed);
                kvp.Value.selectExited.RemoveListener(OnCardReleased);
            }
        }
    }

    private int GetFirstFreeSlot()
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == null) return i;
        }
        return -1;
    }

    private void SnapCard(Card card, int index)
    {

        //----------------------------------------------------------------------------
        // Cambios Emilio: proteccion extra
        //----------------------------------------------------------------------------
        if (!canReceiveNewCards) return;

        int count = GetOccupiedCount();
        bool cardAlreadyInZone = (card.currentZone == this);
        if (!cardAlreadyInZone && count >= Mathf.Max(1, maxCards))
            return;
        //----------------------------------------------------------------------------

        if (logDebug) Debug.Log($"[CardSnapZone:{name}] SnapCard {card.name} en slot {index}");

        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == card)
                occupied[i] = null;
        }

        occupied[index] = card;
        card.currentZone = this;

        Transform t = card.transform;
        t.SetParent(slots[index], worldPositionStays: false);
        t.localPosition = localPositionOffset;
        t.localRotation = Quaternion.Euler(localRotationOffsetEuler);
        t.localScale = card.originalScale;

        card.SetHidden(false);

        // 🔥 OPTIMIZACIÓN: Usar caché en lugar de GetComponentsInParent
        if (cardGrabCache.TryGetValue(card, out XRGrabInteractable grab))
        {
            grab.enabled = canGrabFromZone;
        }

        UpdateSlotVisuals();
        RecalculateScore();

        //----------------------------------------------------------------------------
        // Cambios Emilio
        //----------------------------------------------------------------------------
        OnZoneChanged?.Invoke(this);
        //----------------------------------------------------------------------------

    }

    public void RemoveCard(Card card)
    {
        InternalRemoveCard(card);

        if (card != null && card.currentZone == this)
            card.currentZone = null;

        // Limpiar cachés
        lastCheckTime.Remove(card);
        cardGrabCache.Remove(card);
        
        // Limpiar collider cache
        var collidersToRemove = new List<Collider>();
        foreach (var kvp in colliderToCardCache)
        {
            if (kvp.Value == card)
                collidersToRemove.Add(kvp.Key);
        }
        foreach (var col in collidersToRemove)
        {
            colliderToCardCache.Remove(col);
        }
    }

    private void InternalRemoveCard(Card card)
    {
        if (card == null) return;

        if (logDebug) Debug.Log($"[CardSnapZone:{name}] InternalRemoveCard {card.name}");

        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == card)
            {
                occupied[i] = null;

                Transform t = card.transform;
                if (t != null && t.parent == slots[i])
                {
                    t.SetParent(null, true);
                }

                break;
            }
        }

        UpdateSlotVisuals();
        RecalculateScore();

        //----------------------------------------------------------------------------
        // Cambios Emilio
        //----------------------------------------------------------------------------
        OnZoneChanged?.Invoke(this);
        //----------------------------------------------------------------------------

    }

    public void ResetSlots()
    {
        for (int i = 0; i < occupied.Length; i++)
            occupied[i] = null;

        // Limpiar cachés
        lastCheckTime.Clear();
        cardGrabCache.Clear();
        colliderToCardCache.Clear();

        UpdateSlotVisuals();
        RecalculateScore();
    }

    private void UpdateSlotVisuals()
    {
        if (slotVisuals == null || slotVisuals.Length == 0)
            return;

        //----------------------------------------------------------------------------
        // Cambios Emilio: si se alcanzo el maxCards, no mostrar mas slot (aunque existan mas slots)
        //----------------------------------------------------------------------------
        if (GetOccupiedCount() >= Mathf.Max(1, maxCards))
        {
            for (int i = 0; i < slotVisuals.Length; i++)
            {
                if (slotVisuals[i] != null)
                    slotVisuals[i].enabled = false;
            }
            return;
        }
        //----------------------------------------------------------------------------

        if (!canReceiveNewCards)
        {
            for (int i = 0; i < slotVisuals.Length; i++)
            {
                if (slotVisuals[i] != null)
                    slotVisuals[i].enabled = false;
            }
            return;
        }

        int firstEmpty = GetFirstFreeSlot();

        for (int i = 0; i < slotVisuals.Length; i++)
        {
            if (slotVisuals[i] == null) continue;

            if (firstEmpty == -1)
            {
                slotVisuals[i].enabled = false;
            }
            else
            {
                slotVisuals[i].enabled = (i == firstEmpty);
            }
        }
    }

    private void RecalculateScore()
    {
        int total = 0;
        int aceCount = 0;

        foreach (var card in occupied)
        {
            if (card == null) continue;

            int v = card.GetBlackjackValue();
            total += v;

            if (card.rank == Rank.Ace)
                aceCount++;
        }

        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        if (valueText != null)
            valueText.text = total.ToString();

        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] RecalculateScore -> Total: {total}");
    }

    public IEnumerable<Card> GetCurrentCards()
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null)
                yield return occupied[i];
        }
    }

    public void SetCanGrabFromZone(bool canGrab)
    {
        canGrabFromZone = canGrab;

        // 🔥 OPTIMIZACIÓN: Usar caché
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null && cardGrabCache.TryGetValue(occupied[i], out XRGrabInteractable grab))
            {
                grab.enabled = canGrab;
            }
        }
    }

    public void SetCanReceiveNewCards(bool canReceive)
    {
        canReceiveNewCards = canReceive;
        UpdateSlotVisuals();
    }

    public int GetOccupiedCount()
    {
        int count = 0;
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null) count++;
        }
        return count;
    }

    public bool IsEmpty()
    {
        return GetOccupiedCount() == 0;
    }

    public bool IsFull()
    {
        return GetOccupiedCount() >= occupied.Length;
    }

    public Card GetCardInSlot(int index)
    {
        if (index < 0 || index >= occupied.Length) return null;
        return occupied[index];
    }

    public List<Card> GetAllCards()
    {
        List<Card> list = new List<Card>();
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null)
                list.Add(occupied[i]);
        }
        return list;
    }

    // ========================
    //   MÉTODOS DE CONTROL PARA GAME MANAGER 
    // ========================

    /// <summary>
    /// Bloquea completamente esta zona:
    /// - No acepta nuevas cartas
    /// - No se pueden coger las cartas existentes
    /// </summary>
    public void LockZone()
    {
        SetCanReceiveNewCards(false);
        SetCanGrabFromZone(false);
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Zona bloqueada (locked)");
    }

    /// <summary>
    /// Desbloquea completamente esta zona:
    /// - Acepta nuevas cartas
    /// - Se pueden coger las cartas existentes
    /// </summary>
    public void UnlockZone()
    {
        SetCanReceiveNewCards(true);
        SetCanGrabFromZone(true);
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Zona desbloqueada (unlocked)");
    }

    /// <summary>
    /// Modo "solo lectura": 
    /// - No acepta nuevas cartas
    /// - Las cartas existentes pueden verse pero no cogerse
    /// Útil para mostrar cartas del dealer sin que se puedan mover
    /// </summary>
    public void SetReadOnly(bool readOnly)
    {
        if (readOnly)
        {
            SetCanReceiveNewCards(false);
            SetCanGrabFromZone(false);
            if (logDebug)
                Debug.Log($"[CardSnapZone:{name}] Modo solo lectura activado");
        }
        else
        {
            SetCanReceiveNewCards(true);
            SetCanGrabFromZone(true);
            if (logDebug)
                Debug.Log($"[CardSnapZone:{name}] Modo solo lectura desactivado");
        }
    }

    /// <summary>
    /// Bloquea solo las cartas existentes (no se pueden coger)
    /// pero permite que se añadan nuevas cartas
    /// </summary>
    public void FreezeCards()
    {
        SetCanGrabFromZone(false);
        // canReceiveNewCards se mantiene como está
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Cartas congeladas (no se pueden coger)");
    }

    /// <summary>
    /// Desbloquea las cartas para que se puedan coger
    /// </summary>
    public void UnfreezeCards()
    {
        SetCanGrabFromZone(true);
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Cartas descongeladas (se pueden coger)");
    }

    /// <summary>
    /// Cierra la zona: no acepta más cartas
    /// pero las que hay se pueden coger
    /// </summary>
    public void CloseZone()
    {
        SetCanReceiveNewCards(false);
        // canGrabFromZone se mantiene como está
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Zona cerrada (no acepta más cartas)");
    }

    /// <summary>
    /// Abre la zona: vuelve a aceptar cartas
    /// </summary>
    public void OpenZone()
    {
        SetCanReceiveNewCards(true);
        
        if (logDebug)
            Debug.Log($"[CardSnapZone:{name}] Zona abierta (acepta cartas)");
    }

    /// <summary>
    /// Obtiene el estado actual de la zona
    /// </summary>
    public (bool canReceive, bool canGrab) GetZoneState()
    {
        return (canReceiveNewCards, canGrabFromZone);
    }

    //----------------------------------------------------------------------------
    // Cambios Emilio
    //----------------------------------------------------------------------------
    /// <summary>
    /// Cambia el limite duro de cartas en esta zona (independiente del numero de slots).
    /// </summary>
    public void SetMaxCards(int newMax)
    {
        maxCards = Mathf.Max(1, newMax);
        UpdateSlotVisuals();
    }

    /// <summary>
    /// Devuelve el limite duro actual de cartas.
    /// </summary>
    public int GetMaxCards()
    {
        return Mathf.Max(1, maxCards);
    }
    //----------------------------------------------------------------------------

}