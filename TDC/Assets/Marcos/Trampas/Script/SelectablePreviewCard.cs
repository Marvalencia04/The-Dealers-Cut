using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Hace que la carta sea seleccionable desde ratón, puntero UI o XRGrabInteractable.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class SelectablePreviewCard : MonoBehaviour, IPointerClickHandler
{
    private RandomCard manager;
    private XRGrabInteractable grabInteractable;

    /// <summary>
    /// Inicializa el manager que controla la carta.
    /// </summary>
    public void Init(RandomCard randomCardManager)
    {
        manager = randomCardManager;

        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveAllListeners();
            grabInteractable.selectEntered.AddListener(OnXRSelectEntered);
        }
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
    // CLICK VR / XRGrabInteractable
    // ======================
    private void OnXRSelectEntered(SelectEnterEventArgs args)
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

        Debug.Log($"Carta seleccionada: {name}");
        manager.OnCardSelected(gameObject);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectEntered.RemoveListener(OnXRSelectEntered);
    }
}
