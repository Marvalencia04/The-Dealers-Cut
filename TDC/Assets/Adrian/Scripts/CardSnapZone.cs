using UnityEngine;
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

    private Card[] occupied;
    private bool snapEnabled = true;     // para pausar la zona sin borrar estado

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

    // Mientras la carta esté dentro del trigger
    private void OnTriggerStay(Collider other)
    {
        if (!snapEnabled) return;

        Card card = other.GetComponentInParent<Card>();
        if (card == null) return;

        // Si ya está snappeada en esta zona, no hacemos nada
        if (card.currentZone == this)
            return;

        XRGrabInteractable grab = other.GetComponentInParent<XRGrabInteractable>();

        // Si sigue agarrada con la mano, esperamos a que la suelte
        if (grab != null && grab.isSelected)
            return;

        int index = GetFirstFreeSlot();
        if (index == -1) return; // zona llena

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
