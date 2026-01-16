using UnityEngine;
using TMPro;

public class TeleportModeManager: MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown movementDropdown; // "Continuo" / "Teleport"

    [Header("Locomotion Groups (GameObjects)")]
    [SerializeField] private GameObject moveGroup;          // Locomotion/Move
    [SerializeField] private GameObject teleportGroup;      // Locomotion/Teleportation

    [Header("Interactors (Rays)")]
    [SerializeField] private GameObject leftNearFarInteractor;
    [SerializeField] private GameObject rightNearFarInteractor;
    [SerializeField] private GameObject leftTeleportInteractor;
    [SerializeField] private GameObject rightTeleportInteractor;

    private void Start()
    {
        if (movementDropdown != null)
            movementDropdown.onValueChanged.AddListener(_ => Apply());

        Apply();
    }

    private void Apply()
    {
        string opt = movementDropdown.options[movementDropdown.value].text.Trim().ToLowerInvariant();
        bool isTeleport = opt.Contains("tele") || opt.Contains("tp");

        // Turnarse: solo uno activo
        if (moveGroup != null) moveGroup.SetActive(!isTeleport);
        if (teleportGroup != null) teleportGroup.SetActive(isTeleport);

        // Rays
        if (leftTeleportInteractor != null) leftTeleportInteractor.SetActive(isTeleport);
        if (rightTeleportInteractor != null) rightTeleportInteractor.SetActive(isTeleport);

        if (leftNearFarInteractor != null) leftNearFarInteractor.SetActive(!isTeleport);
        if (rightNearFarInteractor != null) rightNearFarInteractor.SetActive(!isTeleport);
    }
}