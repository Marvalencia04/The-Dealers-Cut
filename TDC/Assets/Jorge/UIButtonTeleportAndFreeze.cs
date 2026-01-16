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

    [Header("Pause")]
    [SerializeField] private bool unpauseGame = true;

    public void OnButtonPressed()
    {
        // 1) Desactivar canvas antiguos
        foreach (var c in canvasesToDisable)
            if (c != null) c.SetActive(false);

        // 2) Activar canvas nuevo
        if (canvasToEnable != null)
            canvasToEnable.SetActive(true);

        // 3) Despausar
        if (unpauseGame)
            Time.timeScale = 1f;

        // 4) Teleport
        if (xrRig != null && teleportTarget != null)
        {
            xrRig.position = teleportTarget.position;
            xrRig.rotation = teleportTarget.rotation;
        }

    }
}
