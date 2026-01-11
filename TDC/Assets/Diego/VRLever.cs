using UnityEngine;
using UnityEngine.Events;


public class VRLever : MonoBehaviour
{
    public HingeJoint hinge;
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;

    public float pullThreshold = -30f; 
    public float resetThreshold = -5f; 

    public bool requireGrab = true; 

    public UnityEvent onPulled;

    private bool fired;

    void Awake()
    {
        if (!hinge) hinge = GetComponent<HingeJoint>();
        if (!grab) grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void Update()
    {
        if (!hinge) return;

        float angle = hinge.angle;
        bool isGrabbed = grab && grab.isSelected;

        if (requireGrab && !isGrabbed)
        {
            TryReset(angle);
            return;
        }

        if (!fired && angle <= pullThreshold)
        {
            fired = true;
            onPulled?.Invoke();
        }

        TryReset(angle);
    }

    private void TryReset(float angle)
    {
        if (fired && angle >= resetThreshold)
            fired = false;
    }
}
