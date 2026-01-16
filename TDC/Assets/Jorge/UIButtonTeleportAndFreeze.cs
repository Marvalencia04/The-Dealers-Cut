using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

public class UIButtonTeleportAndFreeze : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private List<GameObject> canvasesToDisable = new();
    [SerializeField] private GameObject canvasToEnable;

    [Header("Player XR")]
    [SerializeField] private Transform xrRig;
    [SerializeField] private Transform teleportTarget;

    [Header("Auto-find movement in rig")]
    [SerializeField] private bool autoFindMovementComponents = true;

    [Header("Movement Components (optional)")]
    [SerializeField] private LocomotionSystem locomotionSystem;
    [SerializeField] private ContinuousMoveProviderBase moveProvider;

    public void OnButtonPressed()
    {
        // 1️⃣ Desactivar canvas antiguos
        foreach (var c in canvasesToDisable)
        {
            if (c != null)
                c.SetActive(false);
        }

        // 2️⃣ Activar nuevo canvas
        if (canvasToEnable != null)
            canvasToEnable.SetActive(true);

        // 3️⃣ Despausar juego
        Time.timeScale = 1f;

        // 4️⃣ Teletransportar jugador
        if (xrRig != null && teleportTarget != null)
        {
            xrRig.position = teleportTarget.position;
            xrRig.rotation = teleportTarget.rotation;
        }

        // 5️⃣ Buscar componentes de movimiento si hace falta
        if (autoFindMovementComponents && xrRig != null)
        {
            if (locomotionSystem == null)
                locomotionSystem = xrRig.GetComponentInChildren<LocomotionSystem>(true);

            if (moveProvider == null)
                moveProvider = xrRig.GetComponentInChildren<ContinuousMoveProviderBase>(true);
        }

        // 6️⃣ Congelar movimiento
        if (moveProvider != null)
            moveProvider.enabled = false;

        if (locomotionSystem != null)
            locomotionSystem.enabled = false;
    }
}