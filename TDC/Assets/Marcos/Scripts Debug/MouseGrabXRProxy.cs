using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MouseGrabXRProxy : MonoBehaviour
{
    public float moveSpeed = 15f;

    private XRGrabInteractable grab;
    private Camera cam;

    private bool isDragging;
    private Vector3 offset;
    private float zDistance;

    private IXRSelectInteractor mouseInteractor;
    private XRInteractionManager interactionManager;

    private Card card;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        cam = Camera.main;

        card = GetComponent<Card>() ?? GetComponentInChildren<Card>();

        interactionManager = FindObjectOfType<XRInteractionManager>();
        if (interactionManager == null)
        {
            Debug.LogError("[MouseGrabXR] No XRInteractionManager en escena");
            enabled = false;
            return;
        }

        // 🔑 ESTA LÍNEA ES LA CLAVE
        grab.interactionManager = interactionManager;

        GameObject go = new GameObject("MouseXRDirectInteractor");
        var direct = go.AddComponent<XRDirectInteractor>();
        direct.interactionManager = interactionManager;
        mouseInteractor = direct;
        go.SetActive(false);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        grab.throwOnDetach = false;
    }


    void OnMouseDown()
    {
        if (grab == null) return;

        isDragging = true;

        zDistance = Vector3.Distance(cam.transform.position, transform.position);
        offset = transform.position - GetMouseWorldPosition();

        mouseInteractor.transform.position = transform.position;
        mouseInteractor.transform.gameObject.SetActive(true);

        interactionManager.SelectEnter(mouseInteractor, grab);

        Debug.Log($"[MouseGrabXR] Grab -> {card}");
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 targetPos = GetMouseWorldPosition() + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            Time.deltaTime * moveSpeed
        );

        mouseInteractor.transform.position = transform.position;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // 🔑 NO soltar si el SnapZone ya lo ha hecho
        if (grab.isSelected && card != null && card.currentZone == null)
        {
            interactionManager.SelectExit(mouseInteractor, grab);
        }

        mouseInteractor.transform.gameObject.SetActive(false);

        Debug.Log($"[MouseGrabXR] Release -> {card}");
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = zDistance;
        return cam.ScreenToWorldPoint(mousePos);
    }
}
