using UnityEngine;
using UnityEngine.InputSystem;

public class EnableXRLocomotionActions : MonoBehaviour
{
    [SerializeField] private InputActionAsset xriDefaultInputActions;

    private void OnEnable()
    {
        EnableMap("XRI Left Locomotion");
        EnableMap("XRI Right Locomotion");
    }

    private void EnableMap(string mapName)
    {
        var map = xriDefaultInputActions.FindActionMap(mapName, false);

        if (map == null)
        {
            Debug.LogError($"No se encontró el Action Map: {mapName}");
            return;
        }

        if (!map.enabled)
            map.Enable();
    }
}
