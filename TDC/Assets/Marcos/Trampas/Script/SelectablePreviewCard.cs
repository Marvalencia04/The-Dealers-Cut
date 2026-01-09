using UnityEngine;
using UnityEngine.EventSystems;

public class SelectablePreviewCard : MonoBehaviour, IPointerClickHandler
{
    private RandomCard manager;

    /// <summary>
    /// Inicializa el manager que controla la carta
    /// </summary>
    public void Init(RandomCard randomCardManager)
    {
        manager = randomCardManager;
    }

    // ======================
    // CLICK XR / UI
    // ======================
    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked();
    }

    // ======================
    // CLICK MOUSE CLÁSICO
    // ======================
    private void OnMouseDown()
    {
        Clicked();
    }

    // ======================
    // Lógica común
    // ======================
    private void Clicked()
    {
        if (manager == null)
        {
            Debug.LogWarning("SelectablePreviewCard: manager nulo");
            return;
        }

        Debug.Log($"Carta pulsada: {name}");
        manager.OnCardSelected(gameObject);
    }
}
