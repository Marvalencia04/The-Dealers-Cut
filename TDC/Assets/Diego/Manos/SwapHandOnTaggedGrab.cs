using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SwapHandOnTaggedGrab : MonoBehaviour
{
    [Header("Interactor que agarra (Direct / Near-Far, etc.)")]
    [SerializeField] private XRBaseInteractor interactor;

    [Header("Modelos de mano")]
    [SerializeField] private GameObject normalHand;
    [SerializeField] private GameObject cardHand;

    [Header("Tag del objeto que activa la mano carta")]
    [SerializeField] private string cardTag = "Card";

    private void Reset()
    {
        interactor = GetComponent<XRBaseInteractor>();
    }

    private void OnEnable()
    {
        if (!interactor) interactor = GetComponent<XRBaseInteractor>();
        if (!interactor)
        {
            Debug.LogError($"{nameof(SwapHandOnTaggedGrab)}: No hay XRBaseInteractor en este GameObject.");
            enabled = false;
            return;
        }

        interactor.selectEntered.AddListener(OnSelectEntered);
        interactor.selectExited.AddListener(OnSelectExited);

        SetHand(false);
    }

    private void OnDisable()
    {
        if (!interactor) return;
        interactor.selectEntered.RemoveListener(OnSelectEntered);
        interactor.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var t = args.interactableObject.transform;

        // Debug para comprobar que dispara
        Debug.Log($"[SwapHand] Enter: {t.name}");

        if (HasTagInParents(t, cardTag))
            SetHand(true);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var t = args.interactableObject.transform;

        Debug.Log($"[SwapHand] Exit: {t.name}");

        if (HasTagInParents(t, cardTag))
            SetHand(false);
    }

    private bool HasTagInParents(Transform t, string tagName)
    {
        // check en el propio objeto
        if (t.CompareTag(tagName)) return true;

        // check en padres
        var p = t.parent;
        while (p != null)
        {
            if (p.CompareTag(tagName)) return true;
            p = p.parent;
        }
        return false;
    }

    private void SetHand(bool holdingCard)
    {
        if (normalHand) normalHand.SetActive(!holdingCard);
        if (cardHand) cardHand.SetActive(holdingCard);
    }
}
