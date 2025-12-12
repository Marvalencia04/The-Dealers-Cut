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

    [Header("Teleportation Areas to Enable/Disable")]
    // 把场景里所有 Teleportation Area 拖进来
    public TeleportationArea[] teleportationAreas;

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
        //currentRotationMode = (RotationMode)PlayerPrefs.GetInt(RotationKey, 0);
        // 强制一开始就是连续旋转
        currentRotationMode = RotationMode.Continuous;
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

        //  只关掉 TeleportationArea 组件，不禁用整块地板
        if (teleportationAreas != null)
        {
            foreach (var area in teleportationAreas)
            {
                if (area != null)
                    area.enabled = useTeleport;   // ← 关键修改
            }
        }
    }


    private void ApplyRotationMode()
    {
        bool useSnap = (currentRotationMode == RotationMode.Snap);
        Debug.Log($"[Rotation] useSnap = {useSnap}");

        if (snapTurnProvider != null)
        {
            snapTurnProvider.enabled = useSnap;
            Debug.Log($"snapTurnProvider.enabled = {snapTurnProvider.enabled}");
        }

        if (continuousTurnProvider != null)
        {
            continuousTurnProvider.enabled = !useSnap;
            Debug.Log($"continuousTurnProvider.enabled = {continuousTurnProvider.enabled}");
        }
    }

}
