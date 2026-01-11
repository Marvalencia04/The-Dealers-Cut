using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SecurityTarget : MonoBehaviour
{
    [Header("Id del NPC (debe coincidir con el indice/Id que usas en NPCManager y playerZones)")]
    [SerializeField] private int npcId = 0;
    public int NpcId => npcId;

    [Header("Seat (animacion de guardia)")]
    [SerializeField] private SeatGuardKill seatGuardKill;

    [Header("XR (opcional)")]
    [SerializeField] private XRBaseInteractable interactable;

    private void Reset()
    {
        // Intenta encontrar el SeatGuardKill en el padre (normalmente el seat)
        seatGuardKill = GetComponentInParent<SeatGuardKill>();

        // Si este objeto es XR interactable
        interactable = GetComponent<XRBaseInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
            interactable.selectEntered.AddListener(OnXRSelected);
    }

    private void OnDisable()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnXRSelected);
    }

    // Click con raton (Editor / PC). Necesita collider.
    private void OnMouseDown()
    {
        TryNotifyTrap();
    }

    // Select con XR Ray / Direct Interactor
    private void OnXRSelected(SelectEnterEventArgs args)
    {
        TryNotifyTrap();
    }

    private void TryNotifyTrap()
    {
        if (TrampasManager.Instance == null) return;
        TrampasManager.Instance.TrySelectSecurityTarget(this);
    }

    public SeatGuardKill GetSeatGuardKill()
    {
        return seatGuardKill;
    }
}
