using UnityEngine;

public class CardCollector : MonoBehaviour
{
    [Header("Collider ESFERA que usamos para recoger")]
    public Collider collectCollider;

    [Header("Mazo al que mandamos las cartas")]
    public DeckXR deck;

    [Header("Permitir que este jugador recoja cartas")]
    public bool canCollectCards = true;

    private void Start()
    {
        if (collectCollider == null)
            collectCollider = GetComponent<Collider>();

        if (collectCollider != null)
            collectCollider.enabled = false; // apagado al inicio
    }

    public void StartCollecting()
    {
        if (!canCollectCards) return;   // <-- bloqueo aquí

        if (collectCollider != null)
            collectCollider.enabled = true;
    }

    public void StopCollecting()
    {
        if (collectCollider != null)
            collectCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryCollect(other);
    }

    private void TryCollect(Collider other)
    {
        // Si no se puede recoger, ignoramos
        if (!canCollectCards) return;

        if (collectCollider == null || !collectCollider.enabled)
            return;

        Card card = other.GetComponentInParent<Card>();
        if (card == null) return;

        // Sacarla de cualquier zona antes de devolverla
        if (card.currentZone != null)
            card.currentZone.RemoveCard(card);

        deck.ReturnToBottom(card);
    }

    // =====================================
    // MÉTODOS PÚBLICOS PARA EL GAME MANAGER
    // =====================================

    public void SetCanCollectCards(bool value)
    {
        canCollectCards = value;

        // Si acabas de desactivar recoger, apaga la esfera si estaba activa
        if (!value)
            StopCollecting();
    }
}
