using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// ===============================
/// INTERFAZ (la usa BlackjackNPCManager)
/// ===============================
public interface IBlackjackTable
{
    int GetPlayerScore(int playerId);
    bool IsPlayerBust(int playerId);
    bool IsPlayerBlackjack(int playerId);

    void DealCardToPlayer(int playerId);
    void StandPlayer(int playerId);

    bool IsRoundActive { get; }
}

/// ===============================
/// BLACKJACK TABLE
/// ===============================
public class BlackjackTable : MonoBehaviour, IBlackjackTable, IBlackjackTableFlow

{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private RondaManager rondaManager;
    [SerializeField] private UIManager uiManager;

    [Header("NPCs")]
    [SerializeField] private BlackjackNPCManager npcManager;

    [Header("Table Config")]
    [Tooltip("If true, the deck is reshuffled at the start of each round.")]
    [SerializeField] private bool reshuffleEachRound = true;

    [Tooltip("Number of combined decks (1..6 typical casino).")]
    [Range(1, 6)]
    [SerializeField] private int deckCount = 1;

    [Tooltip("Not used by current flow (RondaManager does not wait), kept for future.")]
    [SerializeField] private float visualDelay = 0.25f;

    [SerializeField] private NPCBetText[] npcBetTexts;

    [Header("XR Zones")]
    [SerializeField] private CardSnapZone dealerZone;
    [SerializeField] private CardSnapZone[] playerZones;

    [Header("NPC Turn AI")]
    [SerializeField] private int npcStandThreshold = 17;     // regla basica: hit <= 16, stand >= 17
    [SerializeField] private float decisionPollInterval = 0.1f;




    // ===============================
    // Round/Table State
    // ===============================
    public bool IsRoundActive { get; private set; }

    private Deck deck;

    // NPC hands: playerId -> hand
    private readonly Dictionary<int, Hand> playerHands = new Dictionary<int, Hand>();
    private readonly HashSet<int> playersStood = new HashSet<int>();
    private readonly HashSet<int> playersRemovedBySecurity = new HashSet<int>(); // hand void this round

    // Dealer
    private Hand dealerHand = new Hand();
    private bool dealerSecondCardHidden = true;

    // Bets: playerId -> bet
    private readonly Dictionary<int, int> currentBets = new Dictionary<int, int>();

    // Traps: flags / modes
    private bool ignoreDealerRulesThisRound = false;     // Dealer rule trap
    private bool miraAlliActive = false;                 // "Look there" trap
    private int tableIntegrityStartCount = 0;            // simplified integrity check
    private bool miraAlliPenaltyTriggered = false;

    // "Choose a card" trap: 3 options and a forced next draw
    private Card[] cartaElegirOptions = null;
    private ForcedDraw forcedDraw = ForcedDraw.None;

    private enum ForcedDrawTarget { None, Dealer, Player }
    private enum ForcedDraw { None, NextCardForced }
    private ForcedDrawTarget forcedTarget = ForcedDrawTarget.None;
    private int forcedPlayerId = -1;
    private Card forcedCard;

    // Optional events for UI
    public event Action<Card[]> OnNextThreeCardsShown;
    public event Action<Card[]> OnCartaAElegirPresented;
    public event Action<List<int>> OnSecurityTargetSelectionRequested;
    public event Action<string> OnTableLog;

    private void Awake()
    {
        if (gameManager == null) gameManager = GameManager.Instance;
        if (moneyManager == null) moneyManager = MoneyManager.Instance;
        if (rondaManager == null) rondaManager = RondaManager.Instance;

        if (deck == null) deck = new Deck(deckCount);
    }

    private void OnEnable()
    {
        if (dealerZone != null) dealerZone.OnZoneChanged += HandleZoneChanged;

        if (playerZones != null)
        {
            foreach (var z in playerZones)
                if (z != null) z.OnZoneChanged += HandleZoneChanged;
        }
    }

    private void OnDisable()
    {
        if (dealerZone != null) dealerZone.OnZoneChanged -= HandleZoneChanged;

        if (playerZones != null)
        {
            foreach (var z in playerZones)
                if (z != null) z.OnZoneChanged -= HandleZoneChanged;
        }
    }

    private void HandleZoneChanged(CardSnapZone zone)
    {
        if (rondaManager == null) return;

        // 1) TurnoJugadores: NPCs
        if (rondaManager.CurrentPhase == BlackjackPhase.TurnoJugadores)
        {
            int idx = GetPlayerZoneIndex(zone);
            if (idx >= 0)
            {
                int score = GetZoneScoreFromSnapZone(zone);

                if (score >= 21)
                {
                    zone.SetCanReceiveNewCards(false);
                    zone.SetMaxCards(2);
                }

                ApplyNPCDecisions();
            }
            return;
        }

        // 2) TurnoDealer: Dealer
        if (rondaManager.CurrentPhase == BlackjackPhase.TurnoDealer)
        {
            if (zone == dealerZone)
            {
                ApplyDealerTurnRules();
            }
            return;
        }
    }

    public void StartDealerTurnXR()
    {
        ApplyDealerTurnRules();
    }



    private int GetPlayerZoneIndex(CardSnapZone zone)
    {
        if (playerZones == null) return -1;
        for (int i = 0; i < playerZones.Length; i++)
        {
            if (playerZones[i] == zone)
                return i;
        }
        return -1;
    }

    private int GetZoneScoreFromSnapZone(CardSnapZone zone)
    {
        if (zone == null) return 0;

        int total = 0;
        int aceCount = 0;

        // USAR EL CARD REAL (MonoBehaviour)
        List<global::Card> cards = zone.GetAllCards();

        for (int i = 0; i < cards.Count; i++)
        {
            global::Card c = cards[i];
            if (c == null) continue;

            total += c.GetBlackjackValue();

            // Usar el Rank global del Card.cs
            if (c.rank == global::Rank.Ace)
                aceCount++;
        }

        // Ajuste de Ases (11 -> 1 si nos pasamos)
        while (total > 21 && aceCount > 0)
        {
            total -= 10;
            aceCount--;
        }

        return total;
    }

    private void ApplyDealerTurnRules()
    {
        if (ignoreDealerRulesThisRound)
        {
            // Mientras la trampa esta activa, no aplicamos la norma 17.
            // Solo cerramos si bust / 21.
            int scoreTrap = GetZoneScoreFromSnapZone(dealerZone);
            if (scoreTrap >= 21)
            {
                dealerZone.SetCanReceiveNewCards(false);
                dealerZone.SetMaxCards(2);
                if (rondaManager != null && rondaManager.CurrentPhase == BlackjackPhase.TurnoDealer)
                    rondaManager.OnDealerTurnCompleted();
            }
            else
            {
                // Mantener abierto para que el jugador decida robar o no
                dealerZone.SetCanReceiveNewCards(true);
                dealerZone.SetMaxCards(dealerZone.slots.Length);
            }
            return;
        }


        if (dealerZone == null) return;

        int score = GetZoneScoreFromSnapZone(dealerZone);

        // Si bust o 21 -> cerrar
        if (score >= 21)
        {
            dealerZone.SetCanReceiveNewCards(false);
            dealerZone.SetMaxCards(2);

            //----------------------------------------------------------------------------
            // Cambios: fin automatico del turno del dealer
            //----------------------------------------------------------------------------
            if (rondaManager != null && rondaManager.CurrentPhase == BlackjackPhase.TurnoDealer)
                rondaManager.OnDealerTurnCompleted();
            //----------------------------------------------------------------------------
            return;
        }

        // Regla pedida:
        if (score <= 17)
        {
            dealerZone.SetCanReceiveNewCards(true);
            dealerZone.SetMaxCards(dealerZone.slots.Length);
        }
        else
        {
            dealerZone.SetCanReceiveNewCards(false);
            dealerZone.SetMaxCards(2);

            //----------------------------------------------------------------------------
            // Cambios: fin automatico del turno del dealer
            //----------------------------------------------------------------------------
            if (rondaManager != null && rondaManager.CurrentPhase == BlackjackPhase.TurnoDealer)
                rondaManager.OnDealerTurnCompleted();
            //----------------------------------------------------------------------------
        }
    }

    public bool ApplyNormaDealer_ForceStandNow()
    {
        if (rondaManager == null)
        {
            //Debug.LogError("[BlackjackTable] rondaManager missing.");
            return false;
        }

        // Solo tiene sentido durante TurnoDealer
        if (rondaManager.CurrentPhase != BlackjackPhase.TurnoDealer)
        {
            //Debug.Log("[BlackjackTable] NormaDealer ignored: not in TurnoDealer.");
            return false;
        }

        if (dealerZone == null)
        {
            //Debug.LogError("[BlackjackTable] dealerZone missing.");
            return false;
        }

        int dealerScore = GetZoneScoreFromSnapZone(dealerZone);

        // La trampa permite plantarse si el dealer tiene MENOS de 17
        if (dealerScore >= 17)
        {
           // Debug.Log($"[BlackjackTable] NormaDealer cannot be used: dealerScore={dealerScore} (>=17).");
            return false;
        }

        // 1) Cerrar el dealer: no mas cartas y apagar slots extra
        dealerZone.SetCanReceiveNewCards(false);
        dealerZone.SetMaxCards(2);

        // 2) Saltar directamente a Resultados
        rondaManager.OnDealerTurnCompleted();

       // Debug.Log($"[BlackjackTable] NormaDealer used: force stand at score={dealerScore}. Going to Results.");
        return true;
    }

    public bool AreAllZonesEmpty()
    {
        if (dealerZone == null) return false;
        if (playerZones == null || playerZones.Length == 0) return false;

        if (!dealerZone.IsEmpty())
            return false;

        for (int i = 0; i < playerZones.Length; i++)
        {
            if (playerZones[i] == null) return false;
            if (!playerZones[i].IsEmpty())
                return false;
        }

        return true;
    }


    public void OnSecurityCallApplied(int npcId)
    {
        // Poner apuesta a 0 para que no vuelva a contar en payouts
        if (currentBets.ContainsKey(npcId))
            currentBets[npcId] = 0;

        // Actualizar texto de apuesta (si lo usas)
        NPCBetText bt = GetBetTextForNpc(npcId);
        if (bt != null)
            bt.SetBet(0);
    }




    private bool IsZoneBust(CardSnapZone zone) => GetZoneScoreFromSnapZone(zone) > 21;

    private bool IsZoneBlackjack(CardSnapZone zone)
    {
        if (zone == null) return false;
        if (zone.GetOccupiedCount() != 2) return false;
        return GetZoneScoreFromSnapZone(zone) == 21;
    }

    /// <summary>
    /// Activa el efecto de �Mira All�: todos los jugadores ganan instant�neamente.
    /// </summary>
    public void TriggerMiraAlliPenalty()
    {
        miraAlliPenaltyTriggered = true;

        // ✅ FORZAR ir a Resultados para que el InteractionGate permita recoger cartas
        if (rondaManager != null && rondaManager.CurrentPhase != BlackjackPhase.Resultados)
        {
            // Si no estás en TurnoDealer, igualmente queremos acabar en Resultados
            // Para no depender de la condición interna, hacemos GoToPhase directo.
            rondaManager.GoToPhase(BlackjackPhase.Resultados);
            return; // MUY IMPORTANTE: porque en Resultados ya se llamará a ResolveRoundPayouts()
        }

        // Fallback (si ya estabas en Resultados)
        ResolveBetsAndPayouts();
    }
    public void ResolveResultsFromZonesAndPayout()
    {
        if (moneyManager == null) moneyManager = MoneyManager.Instance;

        int dealerScore = GetZoneScoreFromSnapZone(dealerZone);
        bool dealerBust = dealerScore > 21;
        bool dealerBJ = IsZoneBlackjack(dealerZone);

        //Log($"[RESULTS] Dealer score={dealerScore} bust={dealerBust} blackjack={dealerBJ}");

        foreach (var npc in npcManager.GetNPCs())
        {
            int id = npc.Id;

            // Si fue expulsado por seguridad, su mano se anula (no se paga nada)
            if (npc.IsRemovedBySecurity)
            {
                //Log($"[RESULTS] NPC {id} removed by security -> skip.");
                continue;
            }

            // Apuesta
            int bet = 0;
            currentBets.TryGetValue(id, out bet);
            if (bet <= 0)
            {
                //Log($"[RESULTS] NPC {id} bet=0 -> skip.");
                continue;
            }

            // Zona del NPC por ID (asumimos playerZones[id] corresponde a ese NPC)
            if (id < 0 || id >= playerZones.Length || playerZones[id] == null)
            {
                //Debug.LogWarning($"[BlackjackTable] Missing playerZone for NPC id={id}");
                continue;
            }

            CardSnapZone zone = playerZones[id];

            int playerScore = GetZoneScoreFromSnapZone(zone);
            bool playerBust = playerScore > 21;
            bool playerBJ = IsZoneBlackjack(zone);

            //Log($"[RESULTS] NPC {id}: score={playerScore} bust={playerBust} blackjack={playerBJ} bet={bet}");

            // 1) Si el jugador se pasa -> la casa gana
            if (playerBust)
            {
                moneyManager.DealerWins(bet);
                continue;
            }

            // 2) Blackjack rules
            if (playerBJ && !dealerBJ)
            {
                // NPC blackjack gana
                moneyManager.DealerPaysBlackjack(bet); // paga 3x segun tu MoneyManager
                continue;
            }
            if (dealerBJ && !playerBJ)
            {
                // Dealer blackjack gana: en tu dise�o era DealerWins(bet*2) (ojo, esto es muy agresivo)
                // Para mantener coherencia con lo anterior:
                moneyManager.DealerWins(bet);
                continue;
            }
            if (dealerBJ && playerBJ)
            {
                // Push
                continue;
            }

            // 3) Dealer bust -> pagan todos los que no se pasen
            if (dealerBust)
            {
                moneyManager.DealerPaysWin(bet);
                continue;
            }

            // 4) Comparaci�n normal
            if (playerScore > dealerScore)
            {
                moneyManager.DealerPaysWin(bet);
            }
            else if (playerScore < dealerScore)
            {
                moneyManager.DealerWins(bet);
            }
            else
            {
                // Push: no pasa nada
            }
        }

        //Log("[RESULTS] Payout complete.");
    }





    // ============================================================
    // ========== API CALLED BY RondaManager =======================
    // ============================================================

    public void ResetTableForNewRound()
    {
        IsRoundActive = true;

        playerHands.Clear();
        playersStood.Clear();
        playersRemovedBySecurity.Clear();
        currentBets.Clear();

        dealerHand = new Hand();
        dealerSecondCardHidden = true;

        ignoreDealerRulesThisRound = false;
        miraAlliActive = false;
        miraAlliPenaltyTriggered = false;

        cartaElegirOptions = null;
        forcedDraw = ForcedDraw.None;
        forcedTarget = ForcedDrawTarget.None;
        forcedPlayerId = -1;

        if (deck == null) deck = new Deck(deckCount);
        if (reshuffleEachRound) deck.Shuffle();

        npcManager?.ResetForNewRound();

        if (npcBetTexts != null)
        {
            foreach (var bt in npcBetTexts)
                bt?.Clear();
        }

        // 1) Antes/Despues del reset del npcManager:
        npcManager?.ReplaceRemovedNPCsForNewRound();

        // 2) Desbloquear zonas de jugadores (por si quedaron bloqueadas por seguridad)
        if (playerZones != null)
        {
            for (int i = 0; i < playerZones.Length; i++)
            {
                if (playerZones[i] == null) continue;
                playerZones[i].UnlockZone();

                // al iniciar ronda, que vuelvan a max 2 hasta Reparto
                playerZones[i].SetMaxCards(2);
            }
        }
        if (dealerZone != null)
        {
            dealerZone.UnlockZone();
            dealerZone.SetCanReceiveNewCards(true);
            dealerZone.SetMaxCards(2);
        }


        //Log("Table reset for new round.");
    }

    // Phase 1: Bets (random with minimum)
    public void GenerateBetsForPlayers()
    {
        if (npcManager == null)
        {
            //Debug.LogError("[BlackjackTable] npcManager is missing.");
            return;
        }

        npcManager.GenerateBetsForCurrentRound();

        foreach (var npc in npcManager.GetNPCs())
        {
            currentBets[npc.Id] = npc.CurrentBet;
        }

        //Log("Bets generated.");
    }

    // Phase 2: Initial deal (2 cards each player + dealer)
    public void DealInitialCards()
    {
        EnsureHandsCreated();

        foreach (var id in playerHands.Keys.ToList())
        {
            if (playersRemovedBySecurity.Contains(id)) continue;

            DrawToPlayer(id);
            DrawToPlayer(id);
        }

        DrawToDealer(); // visible
        DrawToDealer(); // hidden until phase 4
        dealerSecondCardHidden = true;

        //Log("Initial cards dealt.");
    }

    // Phase 3: Players turn (NPC hit/stand)
    public void StartPlayersTurn()
    {
        if (npcManager == null)
        {
            //Debug.LogError("[BlackjackTable] npcManager is missing.");
            return;
        }

        npcManager.OnAllNPCsFinishedTurn -= HandleNPCsFinishedTurn;
        npcManager.OnAllNPCsFinishedTurn += HandleNPCsFinishedTurn;

        npcManager.StartNPCsTurn();
        //Log("NPCs turn started.");
    }

    private void HandleNPCsFinishedTurn()
    {
        npcManager.OnAllNPCsFinishedTurn -= HandleNPCsFinishedTurn;

        if (rondaManager != null)
            rondaManager.OnPlayersTurnCompleted();

        //Log("NPCs turn finished.");
    }

    // Phase 4: Reveal dealer second card
    public void RevealDealerSecondCard()
    {
        dealerSecondCardHidden = false;
        //Log("Dealer reveals second card.");
    }

    // Phase 5: Dealer turn (normal vs trap)
    public void PlayDealerTurn()
    {
        dealerSecondCardHidden = false;

        if (!ignoreDealerRulesThisRound)
        {
            while (dealerHand.Score < 17 && !dealerHand.IsBust)
            {
                DrawToDealer();
            }

            //Log("Dealer turn ended (normal). Score=" + dealerHand.Score);
            return;
        }

        while (!dealerHand.IsBust)
        {
            bool shouldHit = DealerShouldHitUnderCheatRule();
            if (shouldHit)
            {
                DrawToDealer();
                continue;
            }
            break;
        }

        ignoreDealerRulesThisRound = false;
        //Log("Dealer turn ended (DealerRule trap). Score=" + dealerHand.Score);
    }

    // Phase 6: Resolve results and payouts
    public void ResolveBetsAndPayouts()
    {
        if (miraAlliPenaltyTriggered)
        {
            foreach (var kv in currentBets)
            {
                int id = kv.Key;
                if (playersRemovedBySecurity.Contains(id)) continue;
                int bet = kv.Value;
                if (bet <= 0) continue;

                moneyManager?.DealerPaysWin(bet);
            }
            

            EndRound();
            //Log("MiraAlli penalty: instant win for all players.");
            return;
        }

        int dealerScore = dealerHand.Score;
        bool dealerBust = dealerHand.IsBust;

        foreach (var kv in currentBets)
        {
            int playerId = kv.Key;
            int bet = kv.Value;
            if (bet <= 0) continue;

            if (playersRemovedBySecurity.Contains(playerId))
                continue;

            var hand = playerHands[playerId];
            bool playerBust = hand.IsBust;

            if (playerBust)
            {
                moneyManager?.DealerWins(bet);
                continue;
            }

            bool playerBJ = hand.IsBlackjack;
            bool dealerBJ = dealerHand.IsBlackjack;

            if (dealerBJ && !playerBJ)
            {
                moneyManager?.DealerWins(bet * 2);
                continue;
            }

            if (playerBJ && !dealerBJ)
            {
                moneyManager?.DealerPaysBlackjack(bet);
                continue;
            }

            if (dealerBust)
            {
                moneyManager?.DealerPaysWin(bet);
            }
            else
            {
                int playerScore = hand.Score;

                if (playerScore > dealerScore)
                    moneyManager?.DealerPaysWin(bet);
                else if (playerScore < dealerScore)
                    moneyManager?.DealerWins(bet);
                else
                {
                    // Push: do nothing
                }
            }
        }
        
        EndRound();
        //Log("Results resolved.");
    }

    private void EndRound()
    {
        IsRoundActive = false;
    }

    public void ComputeBetsForThisRound()
    {
        //Debug.Log("ComputeBetsForThisRound called!");

        // 1) Generar apuestas (aqui se asigna npc.CurrentBet)
        npcManager.GenerateBetsForCurrentRound();

        // 2) Copiar y mostrar en textos
        currentBets.Clear();

        foreach (var npc in npcManager.GetNPCs())
        {
            currentBets[npc.Id] = npc.CurrentBet;

            NPCBetText bt = GetBetTextForNpc(npc.Id);
            if (bt != null)
                bt.SetBet(npc.CurrentBet);
        }
    }


    private NPCBetText GetBetTextForNpc(int npcId)
    {
        if (npcBetTexts == null) return null;

        foreach (var bt in npcBetTexts)
        {
            if (bt != null && bt.NpcId == npcId)
                return bt;
        }
        return null;
    }



    // ============================================================
    // ========== TRAPS API (called by TrampasManager) =============
    // ============================================================

    // Trap: Visible deck - show next 3 cards
    public void ShowNextThreeCards()
    {
        Card[] peek = deck.PeekNext(3);
        OnNextThreeCardsShown?.Invoke(peek);

        //Log("VisibleDeck: " + string.Join(", ", peek.Select(c => c.ToShortString())));
    }

    // Trap: Choose a card - draw 3 options, choose one to force next draw
    public void StartCartaAElegirMode()
    {
        cartaElegirOptions = deck.DrawMany(3);
        OnCartaAElegirPresented?.Invoke(cartaElegirOptions);

        //Log("ChooseCard options: " + string.Join(", ", cartaElegirOptions.Select(c => c.ToShortString())));
    }

    // Called from UI when the player picks an option
    public void ChooseCartaAElegir(int index, bool applyToDealer, int playerIdIfNotDealer)
    {
        if (cartaElegirOptions == null || cartaElegirOptions.Length != 3) return;
        if (index < 0 || index > 2) return;

        forcedCard = cartaElegirOptions[index];
        forcedDraw = ForcedDraw.NextCardForced;

        if (applyToDealer)
        {
            forcedTarget = ForcedDrawTarget.Dealer;
            forcedPlayerId = -1;
            //Log("ChooseCard forced for DEALER: " + forcedCard.ToShortString());
        }
        else
        {
            forcedTarget = ForcedDrawTarget.Player;
            forcedPlayerId = playerIdIfNotDealer;
            //Log("ChooseCard forced for player " + forcedPlayerId + ": " + forcedCard.ToShortString());
        }

        cartaElegirOptions = null; // other 2 are discarded
    }

    // Trap: Security call - request target selection
    public void StartLlamadaSeguridadMode()
    {
        if (npcManager == null) return;

        var ids = npcManager.GetNPCs().Select(n => n.Id).ToList();
        OnSecurityTargetSelectionRequested?.Invoke(ids);

       // Log("SecurityCall: waiting for selection (call ChooseSecurityTarget).");
    }

    // Called from UI when a target is selected
    public void ChooseSecurityTarget(int npcId)
    {
        if (npcManager == null) return;

        bool ok = npcManager.RemoveNPCBySecurity(npcId, out int stolenBet);
        if (!ok) return;

        playersRemovedBySecurity.Add(npcId);

        //Log("Security removed player " + npcId + ". Stolen bet=" + stolenBet + ". Hand void.");
    }

    // Trap: Dealer rule - ignore dealer mandatory hit/stand for this round
    public void EnableIgnoreDealerRulesForThisRound()
    {
        ignoreDealerRulesThisRound = true;
        //Log("DealerRule trap enabled for this round.");
    }

    // Trap: MiraAlli - simplified mode (manual end)
    public void StartMiraAlliMode()
    {
        if (miraAlliActive) return;

        miraAlliActive = true;
        miraAlliPenaltyTriggered = false;

        tableIntegrityStartCount = GetTotalCardsOnTable();
        //Log("MiraAlli started.");
    }

    public void EndMiraAlliMode()
    {
        if (!miraAlliActive) return;

        miraAlliActive = false;

        int endCount = GetTotalCardsOnTable();
        if (endCount != tableIntegrityStartCount)
        {
            miraAlliPenaltyTriggered = true;
            //Log("MiraAlli penalty: card count mismatch.");
        }
        else
        {
           // Log("MiraAlli ended without penalty.");
        }
    }

    // ============================================================
    // ========== IBlackjackTable (NPCManager calls) ===============
    // ============================================================

    public int GetPlayerScore(int playerId)
    {
        if (!playerHands.ContainsKey(playerId)) return 0;
        return playerHands[playerId].Score;
    }

    public bool IsPlayerBust(int playerId)
    {
        if (!playerHands.ContainsKey(playerId)) return false;
        return playerHands[playerId].IsBust;
    }

    public bool IsPlayerBlackjack(int playerId)
    {
        if (!playerHands.ContainsKey(playerId)) return false;
        return playerHands[playerId].IsBlackjack;
    }

    public void DealCardToPlayer(int playerId)
    {
        if (!IsRoundActive) return;
        if (playersRemovedBySecurity.Contains(playerId)) return;
        if (!playerHands.ContainsKey(playerId)) return;

        DrawToPlayer(playerId);
    }

    public void StandPlayer(int playerId)
    {
        playersStood.Add(playerId);
    }

    public void ResetForNewRound()
    {
        ResetTableForNewRound();
    }



    public void StartNPCDecisionTurn()
    {
        if (npcManager == null)
        {
            //Debug.LogError("[BlackjackTable] npcManager missing.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(NPCTurnRoutine());
    }

    private System.Collections.IEnumerator NPCTurnRoutine()
    {
        // Abrimos o cerramos segun decision inicial
        ApplyNPCDecisions();

        // Guardamos un snapshot de conteos para detectar cambios (cuando el jugador coloca cartas)
        int[] lastCounts = new int[playerZones.Length];
        for (int i = 0; i < playerZones.Length; i++)
            lastCounts[i] = playerZones[i] != null ? playerZones[i].GetOccupiedCount() : 0;

        while (rondaManager != null && rondaManager.CurrentPhase == BlackjackPhase.TurnoJugadores)
        {
            // 1) Si algun NPC ha cambiado su numero de cartas, re-evaluamos decisiones
            bool anyChanged = false;
            for (int i = 0; i < playerZones.Length; i++)
            {
                if (playerZones[i] == null) continue;
                int c = playerZones[i].GetOccupiedCount();
                if (c != lastCounts[i])
                {
                    lastCounts[i] = c;
                    anyChanged = true;
                }
            }

            if (anyChanged)
            {
                ApplyNPCDecisions();
            }

            // 2) Si todos los NPCs estan "cerrados" (stand/bust/21), avisamos a RondaManager
            if (AreAllNPCZonesClosed())
            {
                rondaManager.OnPlayersTurnCompleted(); // este metodo lo tienes o lo creas para pasar a la siguiente fase
                yield break;
            }

            yield return new WaitForSeconds(decisionPollInterval);
        }
    }

    private void ApplyNPCDecisions()
    {
        for (int i = 0; i < playerZones.Length; i++)
        {
            CardSnapZone zone = playerZones[i];
            if (zone == null) continue;

            int score = GetZoneScoreFromSnapZone(zone);



            // Si score >= 21, cerramos y apagamos slots sobrantes
            if (score >= 21)
            {
                zone.SetCanReceiveNewCards(false);
                zone.SetMaxCards(2); // o zone.GetOccupiedCount() si quieres que quede exacto
                continue;
            }

            // Si el NPC fue removido por seguridad, lo cerramos
            var npc = npcManager.GetNPCById(i);
            if (npc != null && npc.IsRemovedBySecurity)
            {
                zone.SetCanReceiveNewCards(false);
                zone.SetMaxCards(2);
                continue;
            }

            // Decision hit/stand segun score (regla basica)
            bool wantsHit = DecideNPCHit(i, score);

            if (wantsHit)
            {
                // Abrir zona y permitir usar slots extra
                zone.SetCanReceiveNewCards(true);
                zone.SetMaxCards(zone.slots.Length); // habilita todos los slots disponibles
            }
            else
            {
                // Stand: cerrar para que no reciba mas cartas
                zone.SetCanReceiveNewCards(false);

                // Mantener maxCards en 2 para que no muestre slots extra
                zone.SetMaxCards(2);
            }
        }
    }

    private bool DecideNPCHit(int npcId, int score)
    {
        // Regla base (facil):
        // - Hit si <= 16
        // - Stand si >= 17
        // Puedes mejorar esto usando npc.Risk si quieres.
        return score < npcStandThreshold;
    }

    private bool AreAllNPCZonesClosed()
    {
        for (int i = 0; i < playerZones.Length; i++)
        {
            var z = playerZones[i];
            if (z == null) continue;

            int score = GetPlayerScore(i);

            // Si aun puede recibir cartas y no esta resuelto, no hemos terminado
            if (score < 21 && z.canReceiveNewCards)
                return false;
        }
        return true;
    }



    public void RevealDealerSecondCardInternal()
    {
        RevealDealerSecondCard();
    }

    public void ResolveRoundPayouts()
    {
        ResolveResultsFromZonesAndPayout();
    }



    // ============================================================
    // ========== INTERNAL: draw / hands / dealer ==================
    // ============================================================

    private void EnsureHandsCreated()
    {
        if (npcManager == null) return;

        foreach (var npc in npcManager.GetNPCs())
        {
            if (!playerHands.ContainsKey(npc.Id))
                playerHands[npc.Id] = new Hand();
        }
    }

    private void DrawToPlayer(int playerId)
    {
        Card c = DrawCardConsideringForced(ForcedDrawTarget.Player, playerId);
        playerHands[playerId].Add(c);
        //Log("Player " + playerId + " draws " + c.ToShortString() + " (score=" + playerHands[playerId].Score + ")");
    }

    private void DrawToDealer()
    {
        Card c = DrawCardConsideringForced(ForcedDrawTarget.Dealer, -1);
        dealerHand.Add(c);

        if (dealerHand.Count == 2 && dealerSecondCardHidden)
            Log("Dealer draws [HIDDEN] (visible score=" + GetDealerVisibleScore() + ")");
        else
            Log("Dealer draws " + c.ToShortString() + " (score=" + dealerHand.Score + ")");
    }

    private Card DrawCardConsideringForced(ForcedDrawTarget target, int playerId)
    {
        if (forcedDraw == ForcedDraw.NextCardForced)
        {
            bool matches =
                (forcedTarget == ForcedDrawTarget.Dealer && target == ForcedDrawTarget.Dealer) ||
                (forcedTarget == ForcedDrawTarget.Player && target == ForcedDrawTarget.Player && playerId == forcedPlayerId);

            if (matches)
            {
                forcedDraw = ForcedDraw.None;
                forcedTarget = ForcedDrawTarget.None;
                forcedPlayerId = -1;

                return forcedCard;
            }
        }

        return deck.Draw();
    }

    private int GetDealerVisibleScore()
    {
        if (!dealerSecondCardHidden) return dealerHand.Score;
        if (dealerHand.Count == 0) return 0;
        return dealerHand.GetScoreConsideringHiddenSecondCard();
    }

    private bool DealerShouldHitUnderCheatRule()
    {
        if (dealerHand.IsBust) return false;

        int dealerScore = dealerHand.Score;

        int bestOpponent = 0;
        bool anyOpponentAlive = false;

        foreach (var kv in playerHands)
        {
            int id = kv.Key;
            if (playersRemovedBySecurity.Contains(id)) continue;

            var h = kv.Value;
            if (h.IsBust) continue;

            anyOpponentAlive = true;
            bestOpponent = Mathf.Max(bestOpponent, h.Score);
        }

        if (!anyOpponentAlive) return false;

        if (dealerScore >= bestOpponent && dealerScore <= 21)
            return false;

        if (dealerScore <= 11) return true;
        if (dealerScore >= 20) return false;

        int need = bestOpponent - dealerScore;
        if (need >= 2 && dealerScore <= 16) return true;

        if (dealerScore >= 17 && dealerScore <= 19 && dealerScore < bestOpponent)
            return true;

        return dealerScore < 17;
    }

    private int GetTotalCardsOnTable()
    {
        int total = dealerHand.Count;
        foreach (var h in playerHands.Values) total += h.Count;
        return total;
    }

    private void Log(string msg)
    {
        //Debug.Log("[BlackjackTable] " + msg);
        OnTableLog?.Invoke(msg);
    }

    public bool AreInitialHandsComplete(int requiredCardsPerHand)
    {

        requiredCardsPerHand = Mathf.Max(1, requiredCardsPerHand);

        if (dealerZone == null)
        {
            //Debug.LogError("[BlackjackTable] dealerZone is NULL");
            return false;
        }

        if (playerZones == null || playerZones.Length == 0)
        {
            //Debug.LogError("[BlackjackTable] playerZones is NULL or empty");
            return false;
        }

        int dealerCount = dealerZone.GetOccupiedCount();
        //Debug.Log($"Dealer: {dealerCount}/{requiredCardsPerHand}");

        if (dealerCount < requiredCardsPerHand)
            return false;

        for (int i = 0; i < playerZones.Length; i++)
        {
            if (playerZones[i] == null)
            {
                //Debug.LogError($"PlayerZone {i} is NULL");
                return false;
            }

            int count = playerZones[i].GetOccupiedCount();
            //Debug.Log($"PlayerZone {i}: {count}/{requiredCardsPerHand}");

            if (count < requiredCardsPerHand)
                return false;
        }
        //Debug.Log("AreInitialHandsComplete = TRUE");
        return true;
    }





    // ============================================================
    // ========== INTERNAL TYPES: Card / Hand / Deck ===============
    // ============================================================

    [Serializable]
    public struct Card
    {
        public Suit suit;
        public Rank rank;

        public int BaseValue
        {
            get
            {
                if (rank >= Rank.Two && rank <= Rank.Ten) return (int)rank;
                if (rank == Rank.Ace) return 1;
                return 10;
            }
        }

        public string ToShortString()
        {
            string r;
            if (rank == Rank.Ace) r = "A";
            else if (rank == Rank.Jack) r = "J";
            else if (rank == Rank.Queen) r = "Q";
            else if (rank == Rank.King) r = "K";
            else r = ((int)rank).ToString();

            // No Unicode: use letters
            string s;
            if (suit == Suit.Spades) s = "S";
            else if (suit == Suit.Hearts) s = "H";
            else if (suit == Suit.Diamonds) s = "D";
            else s = "C";

            return r + s;
        }
    }

    public enum Suit { Spades, Hearts, Diamonds, Clubs }

    public enum Rank
    {
        Ace = 1,
        Two = 2, Three = 3, Four = 4, Five = 5, Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10,
        Jack = 11, Queen = 12, King = 13
    }

    public class Hand
    {
        private readonly List<Card> cards = new List<Card>();
        public int Count { get { return cards.Count; } }

        public void Add(Card c) { cards.Add(c); }

        public IReadOnlyList<Card> Cards { get { return cards; } }

        public bool IsBlackjack { get { return (cards.Count == 2 && Score == 21); } }
        public bool IsBust { get { return Score > 21; } }

        public int Score
        {
            get
            {
                int sum = 0;
                int aces = 0;

                foreach (var c in cards)
                {
                    sum += c.BaseValue;
                    if (c.rank == Rank.Ace) aces++;
                }

                while (aces > 0 && sum + 10 <= 21)
                {
                    sum += 10;
                    aces--;
                }

                return sum;
            }
        }

        public int GetScoreConsideringHiddenSecondCard()
        {
            if (cards.Count == 0) return 0;

            Card first = cards[0];
            int sum = first.BaseValue;
            int aces = (first.rank == Rank.Ace) ? 1 : 0;

            while (aces > 0 && sum + 10 <= 21)
            {
                sum += 10;
                aces--;
            }

            return sum;
        }
    }

    public class Deck
    {
        private readonly List<Card> cards = new List<Card>();
        private readonly System.Random rng = new System.Random();

        public Deck(int deckCount)
        {
            Build(deckCount);
            Shuffle();
        }

        private void Build(int deckCount)
        {
            cards.Clear();
            for (int d = 0; d < deckCount; d++)
            {
                foreach (Suit s in Enum.GetValues(typeof(Suit)))
                {
                    foreach (Rank r in Enum.GetValues(typeof(Rank)))
                    {
                        cards.Add(new Card { suit = s, rank = r });
                    }
                }
            }
        }

        public void Shuffle()
        {
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card tmp = cards[k];
                cards[k] = cards[n];
                cards[n] = tmp;
            }
        }

        public Card Draw()
        {
            if (cards.Count == 0)
            {
                Build(1);
                Shuffle();
            }

            int last = cards.Count - 1;
            Card c = cards[last];
            cards.RemoveAt(last);
            return c;
        }

        public Card[] DrawMany(int count)
        {
            Card[] arr = new Card[count];
            for (int i = 0; i < count; i++) arr[i] = Draw();
            return arr;
        }

        public Card[] PeekNext(int count)
        {
            int n = Mathf.Min(count, cards.Count);
            Card[] arr = new Card[n];

            for (int i = 0; i < n; i++)
            {
                arr[i] = cards[cards.Count - 1 - i];
            }
            return arr;
        }
    }
}
