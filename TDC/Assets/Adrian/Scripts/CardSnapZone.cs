using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using TMPro;

public class CardSnapZone : MonoBehaviour
{
    [Header("Posiciones donde se snappean las cartas (en orden)")]
    public Transform[] slots;            // Slot_0, Slot_1, Slot_2, ...

    [Header("Visual de cada slot (mismo ORDEN que slots)")]
    public Renderer[] slotVisuals;       // Visual para cada slot

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

    [Header("UI (puntuación)")]
    public TMP_Text valueText;
    public bool logDebug = false;

    private Card[] occupied;

    private void Awake()
    {
        occupied = new Card[slots.Length];

        // Autodetectar visuales si no los pones a mano
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
            Debug.LogWarning($"[CardSnapZone:{name}] slotVisuals y slots tienen longitudes distintas. " +
                             $"Debes tener MISMO tamaño y ORDEN.");
        }

        UpdateSlotVisuals();
        RecalculateScore();
    }

    private void OnTriggerStay(Collider other)
    {
        Card card = other.GetComponentInParent<Card>();
        if (card == null) return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();
        bool isGrabbed = (grab != null && grab.isSelected);

        // 1) Si la carta está en ESTA zona y ahora la están cogiendo → quitar del slot
        if (card.currentZone == this && isGrabbed)
        {
            if (logDebug) Debug.Log($"[CardSnapZone:{name}] Levantan {card.name} de esta zona");

            InternalRemoveCard(card);           // libera slot + contador + visuales
            card.currentZone = null;
            card.lastGrabTime = Time.time;      // desde ahora aplicamos grace

            return; // este frame NO snappeamos
        }

        // 2) Si pertenece a otra zona distinta, no la tocamos
        if (card.currentZone != null && card.currentZone != this)
            return;

        // 3) A partir de aquí, carta libre (sin zona)

        // Solo queremos autosnap si está en la mano
        if (!isGrabbed)
            return;

        // Si la zona NO acepta nuevas cartas, no snappeamos
        if (!canReceiveNewCards)
            return;

        // 4) Respeta margen de tiempo desde que la levantaste (para poder sacarla)
        float dt = Time.time - card.lastGrabTime;
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

        // 6) Está agarrada, dentro del trigger, ha pasado el grace y está cerca del slot → autosnap
        if (grab != null && grab.isSelected && grab.interactionManager != null)
        {
            var interactors = grab.interactorsSelecting.ToList();
            foreach (var it in interactors)
            {
                grab.interactionManager.SelectExit(it, grab);
            }
        }

        SnapCard(card, index);
    }

    private void OnTriggerExit(Collider other)
    {
        // No necesitamos nada aquí por ahora
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
        if (logDebug) Debug.Log($"[CardSnapZone:{name}] SnapCard {card.name} en slot {index}");

        // Limpiar otros slots que tengan esta carta
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

        // ⬇⬇ NUEVO: revelar la carta al entrar en el slot
        card.SetHidden(false);

        // Ajustar XRGrab según el estado de canGrabFromZone
        XRGrabInteractable[] grabs = card.GetComponentsInParent<XRGrabInteractable>(true);
        foreach (var g in grabs)
        {
            g.enabled = canGrabFromZone;
        }

        UpdateSlotVisuals();
        RecalculateScore();

    }

    /// <summary>
    /// Quita una carta de la zona (cuando se coge o cuando DeckXR la devuelve al mazo).
    /// </summary>
    public void RemoveCard(Card card)
    {
        InternalRemoveCard(card);

        if (card != null && card.currentZone == this)
            card.currentZone = null;
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

                // Si estaba como hija del slot, sacarla al mundo
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
    }

    public void ResetSlots()
    {
        for (int i = 0; i < occupied.Length; i++)
            occupied[i] = null;

        UpdateSlotVisuals();
        RecalculateScore();
    }

    private void UpdateSlotVisuals()
    {
        if (slotVisuals == null || slotVisuals.Length == 0)
            return;

        // 🔴 Si la zona no admite nuevas cartas, apagamos TODOS los visuales y salimos
        if (!canReceiveNewCards)
        {
            for (int i = 0; i < slotVisuals.Length; i++)
            {
                if (slotVisuals[i] != null)
                    slotVisuals[i].enabled = false;
            }
            return;
        }

        int firstEmpty = GetFirstFreeSlot(); // -1 si está llena

        for (int i = 0; i < slotVisuals.Length; i++)
        {
            if (slotVisuals[i] == null) continue;

            if (firstEmpty == -1)
            {
                slotVisuals[i].enabled = false;
            }
            else
            {
                // Solo se enciende el primer slot libre
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

        // Ajuste Ases (11 -> 1) si nos pasamos de 21
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

    // ========================
    //  MÉTODOS PÚBLICOS
    // ========================

    /// <summary>
    /// Activa o desactiva que se puedan coger cartas DEL PLAYERZONE.
    /// </summary>
    public void SetCanGrabFromZone(bool canGrab)
    {
        canGrabFromZone = canGrab;

        // Actualizamos TODOS los XRGrab de las cartas ocupadas ahora mismo
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == null) continue;

            XRGrabInteractable[] grabs = occupied[i].GetComponentsInParent<XRGrabInteractable>(true);
            foreach (var g in grabs)
            {
                g.enabled = canGrab;
            }
        }
    }

    /// <summary>
    /// Activa o desactiva que esta zona acepte cartas nuevas.
    /// </summary>
    public void SetCanReceiveNewCards(bool canReceive)
    {
        canReceiveNewCards = canReceive;
        UpdateSlotVisuals();
    }

    // ========================
    //   MÉTODOS PARA GAME MANAGER 
    // ========================

    /// <summary>
    /// Número de slots que tienen una carta.
    /// </summary>
    public int GetOccupiedCount()
    {
        int count = 0;
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null) count++;
        }
        return count;
    }

    /// <summary>
    /// Devuelve true si NO hay ninguna carta en la zona.
    /// </summary>
    public bool IsEmpty()
    {
        return GetOccupiedCount() == 0;
    }

    /// <summary>
    /// Devuelve true si TODOS los slots están ocupados.
    /// </summary>
    public bool IsFull()
    {
        return GetOccupiedCount() >= occupied.Length;
    }

    /// <summary>
    /// Devuelve la carta que está en un slot concreto (o null).
    /// </summary>
    public Card GetCardInSlot(int index)
    {
        if (index < 0 || index >= occupied.Length) return null;
        return occupied[index];
    }

    /// <summary>
    /// Devuelve una lista de TODAS las cartas actuales.
    /// </summary>
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
}
