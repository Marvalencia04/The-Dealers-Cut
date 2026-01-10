using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controla los jugadores IA del Blackjack:
/// - Genera apuestas aleatorias con mínimo (fase 1)
/// - Ejecuta decisiones HIT/STAND (fase 3)
/// - Soporta la trampa "Llamada de seguridad" (expulsar jugador y anular mano)
/// </summary>
public class BlackjackNPCManager : MonoBehaviour
{
    [Header("Config NPCs")]
    [SerializeField] private int npcCount = 3;
    [SerializeField] private string[] npcNames = { "NPC 1", "NPC 2", "NPC 3", "NPC 4" };

    [Header("Apuestas")]
    [Tooltip("Multiplicador máximo de la apuesta mínima para generar apuestas aleatorias.")]
    [SerializeField] private int maxBetMultiplier = 6;

    [Tooltip("Variación adicional (0..1) aplicada a la apuesta para que no sea tan uniforme.")]
    [Range(0f, 1f)]
    [SerializeField] private float betNoise = 0.25f;

    [Header("IA HIT/STAND")]
    [Tooltip("Puntuación base a partir de la cual el NPC tiende a plantarse (se ajusta con riesgo).")]
    [SerializeField] private int baseStandThreshold = 16;

    [Tooltip("Delay entre decisiones para que se vea natural (y VR-friendly).")]
    [SerializeField] private float decisionDelay = 0.35f;

    [Header("Referencias")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MonoBehaviour blackjackTableComponent; // debe implementar IBlackjackTable

    private IBlackjackTable table;
    private readonly List<BlackjackNPC> npcs = new List<BlackjackNPC>();

    // Evento opcional para UI/mesa (apuestas colocadas, etc.)
    public System.Action<List<BlackjackNPC>> OnBetsGenerated;
    public System.Action OnAllNPCsFinishedTurn;

    private void Awake()
    {
        table = blackjackTableComponent as IBlackjackTable;
        if (table == null)
        {
            Debug.LogError("[BlackjackNPCManager] blackjackTableComponent no implementa IBlackjackTable.");
        }

        if (moneyManager == null) moneyManager = MoneyManager.Instance;
        if (gameManager == null) gameManager = GameManager.Instance;

        BuildNPCs();
    }

    private void BuildNPCs()
    {
        npcs.Clear();
        for (int i = 0; i < npcCount; i++)
        {
            string name = (npcNames != null && npcNames.Length > 0)
                ? npcNames[Mathf.Min(i, npcNames.Length - 1)]
                : $"NPC {i + 1}";

            // riesgo aleatorio suave
            float risk = Random.Range(0.25f, 0.85f);
            npcs.Add(new BlackjackNPC(i, name, risk));
        }
    }

    // ------------------------------------------------------------
    // API pública (la usa BlackjackTable / RondaManager)
    // ------------------------------------------------------------

    public IReadOnlyList<BlackjackNPC> GetNPCs() => npcs;

    /// <summary>Reset de estados de ronda.</summary>
    public void ResetForNewRound()
    {
        foreach (var npc in npcs) npc.ResetForNewRound();
    }

    /// <summary>
    /// Fase 1: genera apuestas aleatorias con mínimo (según el día).
    /// En el GDD: los jugadores apuestan cantidad aleatoria con apuesta mínima. :contentReference[oaicite:4]{index=4}
    /// </summary>
    public void GenerateBetsForCurrentRound()
    {
        int day = gameManager != null ? gameManager.CurrentDay : 1;
        int minBet = moneyManager != null ? moneyManager.GetMinBetForDay(day) : 100;

        foreach (var npc in npcs)
        {
            if (npc.IsRemovedBySecurity) { npc.PlaceBet(0); continue; }

            int maxBet = Mathf.Max(minBet, minBet * Mathf.Max(1, maxBetMultiplier));
            int bet = Random.Range(minBet, maxBet + 1);

            // ruido: a veces reduce un poco, a veces sube un poco
            float noise = Random.Range(-betNoise, betNoise);
            bet = Mathf.RoundToInt(bet * (1f + noise));
            bet = Mathf.Clamp(bet, minBet, maxBet);

            npc.PlaceBet(bet);

            // NOTA: En tu MoneyManager dejamos la apuesta IA como "no afecta hasta resultados"
            // moneyManager.OnPlayerPlacedBet(bet);
        }

        OnBetsGenerated?.Invoke(npcs);
    }

    /// <summary>
    /// Fase 3: ejecuta el turno HIT/STAND de los NPCs.
    /// Cuando todos terminan, dispara OnAllNPCsFinishedTurn.
    /// </summary>
    public void StartNPCsTurn()
    {
        if (table == null)
        {
            Debug.LogError("[BlackjackNPCManager] No hay tabla válida.");
            OnAllNPCsFinishedTurn?.Invoke();
            return;
        }

        StartCoroutine(NPCsTurnRoutine());
    }

    /// <summary>
    /// Trampa "Llamada de seguridad": expulsa al NPC y te quedas su apuesta;
    /// su mano queda anulada esa ronda. :contentReference[oaicite:5]{index=5}
    /// </summary>
    public bool RemoveNPCBySecurity(int npcId, out int stolenBet)
    {
        stolenBet = 0;

        var npc = npcs.FirstOrDefault(n => n.Id == npcId);
        if (npc == null) return false;
        if (npc.IsRemovedBySecurity) return false;

        npc.RemoveBySecurity();
        stolenBet = npc.CurrentBet;

        // La apuesta queda para la casa (sumas dinero inmediatamente)
        if (moneyManager != null && stolenBet > 0)
            moneyManager.AddMoney(stolenBet);

        // La mesa debería también "anular" su mano visualmente (eso lo haces tú en BlackjackTable)
        return true;
    }

    // ------------------------------------------------------------
    // IA de decisiones HIT/STAND
    // ------------------------------------------------------------

    private IEnumerator NPCsTurnRoutine()
    {
        foreach (var npc in npcs)
        {
            if (!table.IsRoundActive) break;

            if (npc.IsRemovedBySecurity)
                continue;

            // Si ya está plantado por algún motivo, saltar
            if (npc.HasStood) continue;

            yield return StartCoroutine(SingleNPCTurn(npc));
        }

        OnAllNPCsFinishedTurn?.Invoke();
    }

    private IEnumerator SingleNPCTurn(BlackjackNPC npc)
    {
        while (table.IsRoundActive)
        {
            if (table.IsPlayerBust(npc.Id))
            {
                npc.MarkStand();
                yield break;
            }

            if (table.IsPlayerBlackjack(npc.Id))
            {
                npc.MarkStand();
                yield break;
            }

            int score = table.GetPlayerScore(npc.Id);

            // Umbral de plantarse: base ajustado por riesgo.
            // Riesgo alto => se planta más tarde (pide más).
            int standThreshold = baseStandThreshold + Mathf.RoundToInt(Mathf.Lerp(-1f, 3f, npc.Risk));
            standThreshold = Mathf.Clamp(standThreshold, 14, 19);

            // Regla simple:
            // - Si score < standThreshold -> HIT (con probabilidad alta)
            // - Si score >= standThreshold -> STAND (con probabilidad alta)
            float pHit;
            if (score < standThreshold)
            {
                // cuanto más lejos de threshold, más probable HIT
                float t = Mathf.InverseLerp(standThreshold - 6, standThreshold, score);
                pHit = Mathf.Lerp(0.95f, 0.55f, t);
            }
            else
            {
                // por encima del threshold casi siempre se planta
                float t = Mathf.InverseLerp(standThreshold, 21, score);
                pHit = Mathf.Lerp(0.35f, 0.05f, t);
            }

            // el riesgo empuja un poco a pedir carta
            pHit = Mathf.Clamp01(pHit + (npc.Risk - 0.5f) * 0.15f);

            bool hit = Random.value < pHit;

            if (hit)
            {
                table.DealCardToPlayer(npc.Id);
                yield return new WaitForSeconds(decisionDelay);

                // si se ha pasado al pedir, se corta en el siguiente loop
            }
            else
            {
                table.StandPlayer(npc.Id);
                npc.MarkStand();
                yield return new WaitForSeconds(decisionDelay);
                yield break;
            }
        }
    }
}
