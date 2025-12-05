using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CardSnapZone : MonoBehaviour
{
    [Header("Posiciones donde se snappean las cartas")]
    public Transform[] slots;            // Slot_1, Slot_2, ...

    [Header("Visual de cada slot (opcional)")]
    public Renderer[] slotVisuals;       // Visual de cada slot (puede ser null)

    [Header("Opciones")]
    [Tooltip("Si está en true, podrás volver a coger la carta después de hacer snap.")]
    public bool canGrabAfterSnap = true;

    [Header("Ajuste fino dentro del slot")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localRotationOffsetEuler = Vector3.zero;

    [Header("Auto snap")]
    [Tooltip("Distancia máxima al slot para que la carta se auto-snapee.")]
    public float snapDistance = 0.08f;

    private Card[] occupied;
    private bool snapEnabled = true;     // para pausar la zona sin borrar estado

    [Header("Blackjack")]
    public BlackjackHand blackjackHand;


    private void Awake()
    {
        occupied = new Card[slots.Length];

        // Si no has rellenado visuales, intenta detectarlos automáticamente
        if (slotVisuals == null || slotVisuals.Length == 0)
        {
            slotVisuals = new Renderer[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slotVisuals[i] = slots[i].GetComponentInChildren<Renderer>(true);
            }
        }

        UpdateSlotVisuals();
    }

    private void OnTriggerStay(Collider other)
{
    if (!snapEnabled) return;

    Card card = other.GetComponentInParent<Card>();
    if (card == null) return;

    XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();

    // 🔹 Si ya está en esta zona y ahora la vuelven a coger, la sacamos de la mano
    if (card.currentZone == this)
    {
        if (grab != null && grab.isSelected)
        {
            RemoveCard(card);
            card.currentZone = null;
        }
        return;
    }

    // 🔹 A partir de aquí es una carta que aún NO está snappeada en esta zona

    // Solo miramos el PRIMER slot libre (el resaltado)
    int index = GetFirstFreeSlot();
    if (index == -1) return; // zona llena

    Transform targetSlot = slots[index];
    if (targetSlot == null) return;

    // Distancia de la carta al slot que toca
    float distance = Vector3.Distance(card.transform.position, targetSlot.position);

    // Si está demasiado lejos del slot, no snappeamos
    if (distance > snapDistance) return;

    // Si la carta sigue agarrada, la soltamos del interactor
    if (grab != null && grab.isSelected && grab.interactionManager != null)
    {
        var interactors = grab.interactorsSelecting.ToList();
        foreach (var interactor in interactors)
        {
            grab.interactionManager.SelectExit(interactor, grab);
        }
    }

    // Ahora sí, snappeamos en ESE slot (el primero libre)
    SnapCard(card, grab, index);
}



    // Cuando la carta SALE de esta zona, liberamos el hueco
    private void OnTriggerExit(Collider other)
    {
        Card card = other.GetComponentInParent<Card>();
        if (card == null) return;

        if (card.currentZone == this)
        {
            RemoveCard(card);
            card.currentZone = null;
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

    // 🔍 Nuevo: buscar el slot libre más cercano a la carta
    private int GetClosestFreeSlotIndex(Vector3 cardPosition, out float minDistance)
    {
        int bestIndex = -1;
        minDistance = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (occupied[i] != null) continue; // ya hay carta

            float dist = Vector3.Distance(cardPosition, slots[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void SnapCard(Card card, XRGrabInteractable grab, int index)
    {
        occupied[index] = card;
        card.currentZone = this;

        Transform t = card.transform;

        // Hacerla hija del slot y alinearla
        t.SetParent(slots[index], worldPositionStays: false);
        t.localPosition = localPositionOffset;
        t.localRotation = Quaternion.Euler(localRotationOffsetEuler);
        t.localScale = card.originalScale;

        if (blackjackHand != null)
        {
            blackjackHand.AddCard(card);
        }

        // Si NO quieres que se pueda volver a coger, desactiva el grab
        if (!canGrabAfterSnap && grab != null)
        {
            grab.enabled = false;
        }

        UpdateSlotVisuals();
    }

    public void RemoveCard(Card card)
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] == card)
            {
                occupied[i] = null;
                break;
            }
        }

        if (blackjackHand != null)
        {
            blackjackHand.RemoveCard(card);
        }

        UpdateSlotVisuals();
    }

    // Borrar TODO el estado (para nueva ronda)
    public void ResetSlots()
    {
        for (int i = 0; i < occupied.Length; i++)
            occupied[i] = null;

        UpdateSlotVisuals();
    }

    // Siempre enciende SOLO el primer slot libre
    private void UpdateSlotVisuals()
    {
        if (slotVisuals == null || slotVisuals.Length == 0)
            return;

        int firstEmpty = GetFirstFreeSlot(); // -1 si está lleno

        for (int i = 0; i < slotVisuals.Length; i++)
        {
            if (slotVisuals[i] == null) continue;

            if (firstEmpty == -1)
            {
                // No hay huecos libres → apaga todos
                slotVisuals[i].enabled = false;
            }
            else
            {
                // Solo se enciende el primer slot libre
                slotVisuals[i].enabled = (i == firstEmpty);
            }
        }
    }

    // 🔒 Bloquear / desbloquear levantar cartas de los slots (sin tocar estado)
    public void LockSlotCards(bool locked)
    {
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i] != null)
            {
                XRGrabInteractable grab = occupied[i].GetComponentInParent<XRGrabInteractable>();
                if (grab != null)
                    grab.enabled = !locked;  // locked = true → no se puede coger
            }
        }
    }

    // ⏸️ Pausar / reanudar el snap sin perder cartas ni ocupados
    public void SetZoneActive(bool active, bool hideVisualsWhenOff = true)
    {
        snapEnabled = active;

        if (!active && hideVisualsWhenOff && slotVisuals != null)
        {
            // Oculta todos los visuales, pero NO borra occupied
            for (int i = 0; i < slotVisuals.Length; i++)
            {
                if (slotVisuals[i] != null)
                    slotVisuals[i].enabled = false;
            }
        }

        if (active)
        {
            // Al reactivar, recalcula el primer hueco libre según las cartas que haya
            UpdateSlotVisuals();
        }
    }

    // Para OnClick: Pausar zona (sin borrar estado)
    public void DisableZone()
    {
        SetZoneActive(false, true);
    }

    // Para OnClick: Activar zona
    public void EnableZone()
    {
        SetZoneActive(true, true);
    }

}
