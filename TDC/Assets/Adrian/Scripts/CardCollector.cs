using UnityEngine;

public class CardCollector : MonoBehaviour
{
    public Collider collectCollider;
    public DeckXR deck;

    private void Start()
    {
        collectCollider.enabled = false;
    }

    public void StartCollecting()
    {
        collectCollider.enabled = true;
    }

    public void StopCollecting()
    {
        collectCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!collectCollider.enabled) return;

        Card card = other.GetComponentInParent<Card>();
        if (card == null) return;

        deck.ReturnToBottom(card);
    }
}
