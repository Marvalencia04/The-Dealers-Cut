using UnityEngine;

/// <summary>
/// Gestiona la recompensa de trampas desde la tragaperras.
/// </summary>
public class SlotTrapRewardManager : MonoBehaviour
{
    [Header("Configuración de probabilidades de rareza (en %)")]
    [Range(0, 100)] public int probComun = 60;
    [Range(0, 100)] public int probRara = 25;
    [Range(0, 100)] public int probEpica = 10;
    [Range(0, 100)] public int probLegendaria = 5;

    /// <summary>
    /// Llama a TrampasManager para dar una trampa según rareza aleatoria.
    /// </summary>
    public void GiveTrapReward()
    {
        if (TrampasManager.Instance == null)
        {
            Debug.LogError("❌ TrampasManager no encontrado.");
            return;
        }

        TrapRarity rarity = GetRandomRarity();
        Debug.Log($"🎰 Premio de rareza: {rarity}");

        TrampasManager.Instance.RecoverTrap(rarity);
        TrampasManager.Instance.LogUsosActuales();
    }

    /// <summary>
    /// Genera rareza aleatoria según las probabilidades configuradas.
    /// </summary>
    private TrapRarity GetRandomRarity()
    {
        int roll = Random.Range(1, 101); // 1 a 100
        int acc = 0;

        acc += probComun;
        if (roll <= acc) return TrapRarity.Comun;

        acc += probRara;
        if (roll <= acc) return TrapRarity.Rara;

        acc += probEpica;
        if (roll <= acc) return TrapRarity.Epica;

        return TrapRarity.Legendaria;
    }
}
