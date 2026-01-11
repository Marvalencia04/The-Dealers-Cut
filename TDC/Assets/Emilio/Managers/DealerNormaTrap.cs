using UnityEngine;

/// <summary>
/// Trampa: Norma Dealer
/// Solo funcionalidad: permite al dealer plantarse con <17 forzando fin del turno.
/// No valida fase/usos/UI (eso lo hace TrampasManager).
/// </summary>
public class DealerNormaTrap : MonoBehaviour
{
    [SerializeField] private BlackjackTable blackjackTable;

    public bool Apply()
    {
        if (blackjackTable == null)
        {
            Debug.LogError("[DealerNormaTrap] blackjackTable missing.");
            return false;
        }

        return blackjackTable.ApplyNormaDealer_ForceStandNow();
    }
}
