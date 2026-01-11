using UnityEngine;

public class LlamadaSeguridadTrap : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BlackjackTable blackjackTable;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private BlackjackNPCManager npcManager;

    [Header("XR Zones")]
    [SerializeField] private CardSnapZone[] playerZones;

    private bool selecting = false;

    public bool IsSelecting => selecting;

    private void Awake()
    {
        if (moneyManager == null) moneyManager = MoneyManager.Instance;
    }

    public void BeginSelection()
    {
        selecting = true;
        Debug.Log("[LlamadaSeguridadTrap] Selection mode ON. Click an NPC.");
    }

    public void CancelSelection()
    {
        selecting = false;
        Debug.Log("[LlamadaSeguridadTrap] Selection mode OFF.");
    }

    // Devuelve true si se aplico (para consumir uso)
    public bool TryApply(SecurityTarget target)
    {
        if (!selecting) return false;
        if (target == null) return false;

        int npcId = target.NpcId;

        // 1) Validar
        if (npcManager == null)
        {
            Debug.LogWarning("[LlamadaSeguridadTrap] npcManager missing.");
            return false;
        }

        var npc = npcManager.GetNPCById(npcId);
        if (npc == null)
        {
            Debug.LogWarning("[LlamadaSeguridadTrap] NPC not found for id=" + npcId);
            return false;
        }

        if (npc.IsRemovedBySecurity)
        {
            Debug.Log("[LlamadaSeguridadTrap] NPC already removed. Ignoring.");
            return false;
        }

        // 2) Ejecutar animacion del guardia (seat)
        SeatGuardKill kill = target.GetSeatGuardKill();
        if (kill != null)
            kill.Execute(); // usa tu sistema actual :contentReference[oaicite:2]{index=2}
        else
            Debug.LogWarning("[LlamadaSeguridadTrap] SeatGuardKill not assigned on target " + target.name);

        // 3) Marcar NPC como expulsado y obtener apuesta robada
        int stolenBet = 0;
        bool ok = npcManager.RemoveNPCBySecurity(npcId, out stolenBet);
        if (!ok)
        {
            Debug.LogWarning("[LlamadaSeguridadTrap] RemoveNPCBySecurity failed for id=" + npcId);
            return false;
        }

        // 4) Bloquear su PlayerZone
        if (playerZones != null && npcId >= 0 && npcId < playerZones.Length && playerZones[npcId] != null)
        {
            playerZones[npcId].LockZone();
        }
        else
        {
            Debug.LogWarning("[LlamadaSeguridadTrap] playerZones not configured or id out of range.");
        }

        // 5) Sumar dinero inmediatamente (robas su apuesta)
        if (moneyManager != null && stolenBet > 0)
        {
            moneyManager.AddMoney(stolenBet);
        }

        // 6) (Opcional) Poner apuesta a 0 en UI/table
        if (blackjackTable != null)
        {
            blackjackTable.OnSecurityCallApplied(npcId);
        }

        selecting = false;

        Debug.Log("[LlamadaSeguridadTrap] Applied to NPC " + npcId + ", stolenBet=" + stolenBet);
        return true;
    }
}
