using UnityEngine;

public class NPCGroupSeat : MonoBehaviour
{
    [Header("Los 3 NPCs de este asiento (solo 1 debe estar activo)")]
    [SerializeField] private GameObject[] npcs;

    public GameObject GetActiveNPC()
    {
        if (npcs == null) return null;

        foreach (var npc in npcs)
        {
            if (npc && npc.activeSelf)
                return npc;
        }
        return null;
    }

    public void DeactivateActiveNPC()
    {
        var active = GetActiveNPC();
        if (active) active.SetActive(false);
    }

    // Opcional: activar uno concreto (para variar durante la partida)
    public void ActivateIndex(int index)
    {
        if (npcs == null || npcs.Length == 0) return;

        for (int i = 0; i < npcs.Length; i++)
        {
            if (npcs[i])
                npcs[i].SetActive(i == index);
        }
    }
}
