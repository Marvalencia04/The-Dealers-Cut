using UnityEngine;

/// <summary>
/// Gestiona las recompensas de trampas de la tragaperras.
/// Decide la rareza y notifica al TrampasManager.
/// </summary>
public class SlotTrapReward : MonoBehaviour
{
    [Header("Probabilidades de rareza (suman 100)")]
    [Range(0, 100)] public int comun = 60;
    [Range(0, 100)] public int rara = 25;
    [Range(0, 100)] public int epica = 10;
    [Range(0, 100)] public int legendaria = 5;

    /// <summary>
    /// Llamado cuando la tragaperras da premio.
    /// </summary>
    public void GiveTrapReward()
    {
        if (TrampasManager.Instance == null)
        {
            Debug.LogError("❌ SlotTrapReward: TrampasManager no encontrado.");
            return;
        }

        TrapRarity rarity = RollRarity();

        Debug.Log($"🎰 Tragaperras → Rareza obtenida: {rarity}");

        TrampasManager.Instance.RecoverTrap(rarity);
    }

    private TrapRarity RollRarity()
    {
        int roll = Random.Range(0, 100);
        int acumulado = 0;

        acumulado += comun;
        if (roll < acumulado) return TrapRarity.Comun;

        acumulado += rara;
        if (roll < acumulado) return TrapRarity.Rara;

        acumulado += epica;
        if (roll < acumulado) return TrapRarity.Epica;

        return TrapRarity.Legendaria;
    }
}
