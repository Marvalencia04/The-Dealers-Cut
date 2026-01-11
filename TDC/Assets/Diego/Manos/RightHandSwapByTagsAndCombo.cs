using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RightHandSwapByTagsAndCombo : MonoBehaviour
{
    [Header("Interactor (mano derecha)")]
    [SerializeField] private XRBaseInteractor interactor;

    [Header("Input (mano derecha)")]
    [SerializeField] private InputActionProperty triggerAction;
    [SerializeField] private InputActionProperty gripAction;

    [Header("Prefabs / modelos (hijos del Right Controller Visual)")]
    [SerializeField] private GameObject normalHand;
    [SerializeField] private GameObject cardHand;
    [SerializeField] private GameObject leverHand;   // mano especial para palanca

    [Header("Tags")]
    [SerializeField] private string cardTag = "Card";
    [SerializeField] private string leverTag = "Palanca";

    [Header("Threshold (0..1)")]
    [Range(0f, 1f)] [SerializeField] private float threshold = 0.6f;

    // Estado
    private bool holdingCard;
    private bool holdingLever;
    private bool triggerPressed;
    private bool gripPressed;

    private void Reset()
    {
        interactor = GetComponent<XRBaseInteractor>();
    }

    private void OnEnable()
    {
        if (!interactor) interactor = GetComponent<XRBaseInteractor>();
        if (!interactor)
        {
            Debug.LogError($"{nameof(RightHandSwapByTagsAndCombo)}: No hay XRBaseInteractor en este objeto.");
            enabled = false;
            return;
        }

        interactor.selectEntered.AddListener(OnSelectEntered);
        interactor.selectExited.AddListener(OnSelectExited);

        triggerAction.action?.Enable();
        gripAction.action?.Enable();

        if (triggerAction.action != null)
        {
            triggerAction.action.performed += OnTriggerChanged;
            triggerAction.action.canceled  += OnTriggerChanged;
        }
        if (gripAction.action != null)
        {
            gripAction.action.performed += OnGripChanged;
            gripAction.action.canceled  += OnGripChanged;
        }

        ApplyHandState();
    }

    private void OnDisable()
    {
        if (interactor != null)
        {
            interactor.selectEntered.RemoveListener(OnSelectEntered);
            interactor.selectExited.RemoveListener(OnSelectExited);
        }

        if (triggerAction.action != null)
        {
            triggerAction.action.performed -= OnTriggerChanged;
            triggerAction.action.canceled  -= OnTriggerChanged;
            triggerAction.action.Disable();
        }
        if (gripAction.action != null)
        {
            gripAction.action.performed -= OnGripChanged;
            gripAction.action.canceled  -= OnGripChanged;
            gripAction.action.Disable();
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var t = args.interactableObject.transform;

        holdingCard  = HasTagInParents(t, cardTag);
        holdingLever = HasTagInParents(t, leverTag);

        ApplyHandState();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var t = args.interactableObject.transform;

        // Solo limpiamos el estado si lo que se suelta era de ese tipo
        if (HasTagInParents(t, cardTag)) holdingCard = false;
        if (HasTagInParents(t, leverTag)) holdingLever = false;

        ApplyHandState();
    }

    private void OnTriggerChanged(InputAction.CallbackContext ctx)
    {
        triggerPressed = ctx.ReadValue<float>() >= threshold;
        ApplyHandState();
    }

    private void OnGripChanged(InputAction.CallbackContext ctx)
    {
        gripPressed = ctx.ReadValue<float>() >= threshold;
        ApplyHandState();
    }

    private void ApplyHandState()
    {
        // PRIORIDAD:
        // 1) Palanca + (Trigger&Grip) -> leverHand
        // 2) Si no, si estás agarrando carta -> cardHand
        // 3) Si no -> normalHand

        bool leverCombo = holdingLever && triggerPressed && gripPressed;

        if (normalHand) normalHand.SetActive(!leverCombo && !holdingCard);
        if (cardHand)   cardHand.SetActive(!leverCombo && holdingCard);
        if (leverHand)  leverHand.SetActive(leverCombo);
    }

    private bool HasTagInParents(Transform t, string tagName)
    {
        if (t.CompareTag(tagName)) return true;
        var p = t.parent;
        while (p != null)
        {
            if (p.CompareTag(tagName)) return true;
            p = p.parent;
        }
        return false;
    }
}
