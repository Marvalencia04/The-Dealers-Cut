using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

public class LocomotionAccessibilityMenu : MonoBehaviour
{
    [Header("Locomotion Providers")]
    public TeleportationProvider teleportationProvider;
    public ContinuousMoveProvider continuousMoveProvider;

    [Header("Turn Providers")]
    public SnapTurnProvider snapTurnProvider;
    public ContinuousTurnProvider continuousTurnProvider;

    public enum LocomotionMode
    {
        Teleport = 0,
        Continuous = 1
    }

    public enum RotationMode
    {
        Snap = 0,
        Continuous = 1
    }

    private LocomotionMode currentLocomotionMode;
    private RotationMode currentRotationMode;

    private const string LocomotionKey = "Accessibility_LocomotionMode";
    private const string RotationKey = "Accessibility_RotationMode";

    void Start()
    {
        currentLocomotionMode = (LocomotionMode)PlayerPrefs.GetInt(LocomotionKey, 0);
        currentRotationMode = (RotationMode)PlayerPrefs.GetInt(RotationKey, 0);

        ApplyLocomotionMode();
        ApplyRotationMode();
    }

    public void SetLocomotionMode(int index)
    {
        currentLocomotionMode = (LocomotionMode)index;
        ApplyLocomotionMode();

        PlayerPrefs.SetInt(LocomotionKey, index);
        PlayerPrefs.Save();
    }

    public void SetRotationMode(int index)
    {
        currentRotationMode = (RotationMode)index;
        ApplyRotationMode();

        PlayerPrefs.SetInt(RotationKey, index);
        PlayerPrefs.Save();
    }

    private void ApplyLocomotionMode()
    {
        bool useTeleport = (currentLocomotionMode == LocomotionMode.Teleport);

        if (teleportationProvider != null)
            teleportationProvider.enabled = useTeleport;

        if (continuousMoveProvider != null)
            continuousMoveProvider.enabled = !useTeleport;
    }

    private void ApplyRotationMode()
    {
        bool useSnap = (currentRotationMode == RotationMode.Snap);

        if (snapTurnProvider != null)
            snapTurnProvider.enabled = useSnap;

        if (continuousTurnProvider != null)
            continuousTurnProvider.enabled = !useSnap;
    }
}
