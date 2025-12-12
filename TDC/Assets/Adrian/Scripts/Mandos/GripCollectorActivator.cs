using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GripCollectorActivator : MonoBehaviour
{
    [Tooltip("CardCollector de la esfera de esta mano")]
    public CardCollector collector;

    [Tooltip("Si el grip está mapeado a Activate (true) o a Select (false)")]
    public bool gripUsaActivate = true;

    private XRBaseController controller;
    private bool lastPressed = false;

    private void Awake()
    {
        controller = GetComponent<XRBaseController>();
    }

    private void Update()
    {
        if (controller == null || collector == null) return;

        bool pressed = gripUsaActivate
            ? controller.activateInteractionState.active
            : controller.selectInteractionState.active;

        if (pressed && !lastPressed)
            collector.StartCollecting();
        else if (!pressed && lastPressed)
            collector.StopCollecting();

        lastPressed = pressed;
    }
}
