using UnityEngine;
using TMPro; // si usas TextMeshPro

public class NPCBetText : MonoBehaviour
{
    [SerializeField] private int npcId;
    [SerializeField] private TMP_Text betText;

    private void Awake()
    {
        if (betText == null)
            betText = GetComponent<TMP_Text>();
    }

    public int NpcId => npcId;

    public void SetBet(int amount)
    {
        if (betText == null) return;

        betText.text = $"Bet: ${amount}";
        betText.gameObject.SetActive(true);
        //Debug.Log($"NPC {npcId} bet set to {amount}");
    }

    public void Clear()
    {
        if (betText == null) return;

        betText.text = "";
        betText.gameObject.SetActive(false);
    }
}
