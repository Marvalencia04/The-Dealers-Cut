using UnityEngine;

public class SelectablePreviewCard : MonoBehaviour
{
    private RandomCard manager;

    public void Init(RandomCard randomCardManager)
    {
        manager = randomCardManager;
    }

    private void OnMouseDown()
    {
        if (manager != null)
            manager.OnCardSelected(gameObject);
    }
}
