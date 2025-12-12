using UnityEngine;
using UnityEngine.InputSystem;

public class GripCollectorInput : MonoBehaviour
{
    public CardCollector collector;
    public InputActionProperty gripAction;   // acción del grip del mando

    private bool collecting = false;

    private void OnEnable()
    {
        gripAction.action.Enable();
    }

    private void OnDisable()
    {
        gripAction.action.Disable();
    }

    private void Update()
    {
        float value = gripAction.action.ReadValue<float>();
        bool pressed = value > 0.5f; // umbral del grip

        if (pressed && !collecting)
        {
            collector.StartCollecting();
            collecting = true;
        }
        else if (!pressed && collecting)
        {
            collector.StopCollecting();
            collecting = false;
        }
    }
}
