using UnityEngine;
using System;

/// <summary>
/// Fases internas de una ronda de Blackjack.
/// Player-driven: SOLO cambia de fase cuando el jugador lo ordena.
/// </summary>
public enum BlackjackPhase
{
    None,
    Apuestas,
    Reparto,
    TurnoJugadores,
    RevelarSegundaCarta,
    TurnoDealer,
    Resultados
}

public class RondaManager : MonoBehaviour
{
    public static RondaManager Instance { get; private set; }

    [Header("Rondas por día")]
    [SerializeField] private int defaultRoundsPerDay = 5;

    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TrampasManager trampasManager;

    [Tooltip("Gate que habilita/bloquea interacciones VR según fase (DeckXR, recoger, zonas...).")]
    [SerializeField] private BlackjackInteractionGateXR interactionGate;

    [Tooltip("Mesa XR o controlador de blackjack que hace cálculos internos (apuestas, resultados, revelar carta...).")]
    [SerializeField] private MonoBehaviour blackjackTableComponent;

    // Si tu mesa implementa estas funciones (las que venimos usando)
    private IBlackjackTableFlow tableFlow;

    // Estado de día
    [SerializeField] private int totalRoundsPerDay;
    [SerializeField] private int currentRound;

    // Estado de fase
    [SerializeField] private BlackjackPhase currentPhase = BlackjackPhase.None;

    [SerializeField] private bool autoAdvanceAfterBets = true;

    [Header("Auto skip phases (temporary)")]
    [SerializeField] private bool autoSkipRevealDealerSecondCard = true;

    [SerializeField] private float autoSkipRevealDelay = 0.05f; // pequeno delay para que UI se refresque

    [Header("Resultados - Limpieza de mesa")]
    [SerializeField] private float pollClearTableInterval = 0.2f;
    [SerializeField] private float minResultsTimeBeforeAutoNext = 0.5f; // evita saltar instantaneo por error
    private Coroutine waitClearRoutine;


    public BlackjackPhase CurrentPhase => currentPhase;

    public int CurrentRound => currentRound;
    public int TotalRoundsPerDay => totalRoundsPerDay;

    public event Action<BlackjackPhase> OnPhaseChanged;
    public event Action<int> OnRoundStarted;
    public event Action<int> OnRoundEnded;
    private Coroutine dealCheckCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (gameManager == null) gameManager = GameManager.Instance;

        // Casteo a "flow" (opcional, pero recomendado)
        tableFlow = blackjackTableComponent as IBlackjackTableFlow;
        if (blackjackTableComponent != null && tableFlow == null)
        {
            Debug.LogWarning("[RondaManager] blackjackTableComponent no implementa IBlackjackTableFlow. " +
                             "Podrás cambiar fases, pero no ejecutar cálculos de apuestas/resultados.");
        }
    }

    // ----------------------------------------------------------------------
    // Configuración por día (llamado por GameManager)
    // ----------------------------------------------------------------------

    public void SetupForNewDay(int rounds)
    {
        totalRoundsPerDay = rounds > 0 ? rounds : defaultRoundsPerDay;
        currentRound = 0;
        currentPhase = BlackjackPhase.None;

        Debug.Log($"[RondaManager] Nuevo día configurado. Rondas: {totalRoundsPerDay}");
    }

    public void StartFirstRound()
    {
        StartNextRound();
    }

    public void StartNextRound()
    {
        currentRound++;

        if (currentRound > totalRoundsPerDay)
        {
            Debug.Log("[RondaManager] Día terminado: todas las rondas completadas.");
            gameManager?.OnAllRoundsFinished();
            return;
        }

        Debug.Log($"[RondaManager] Ronda {currentRound}/{totalRoundsPerDay} iniciada.");

        // Reseteo interno de la mesa (cartas, estados, etc.)
        tableFlow?.ResetForNewRound();

        // UI
        uiManager?.ShowRoundIntro(currentRound, totalRoundsPerDay);

        OnRoundStarted?.Invoke(currentRound);

        // Entramos en apuestas (cálculo interno + permisos)
        GoToPhase(BlackjackPhase.Apuestas);
    }

    // ----------------------------------------------------------------------
    // Cambio de fase (solo aquí se cambia)
    // ----------------------------------------------------------------------

    public void GoToPhase(BlackjackPhase newPhase)
    {
        currentPhase = newPhase;

        uiManager?.UpdateBlackjackPhase(currentPhase);
        trampasManager?.OnBlackjackPhaseChanged(currentPhase);
        interactionGate?.ApplyPhase(currentPhase);

        OnPhaseChanged?.Invoke(currentPhase);

        switch (currentPhase)
        {
            case BlackjackPhase.Apuestas:

                // 1) UI de ronda SIEMPRE que entramos en apuestas (garantiza refresco)
                if (uiManager != null)
                    uiManager.ShowRoundIntro(currentRound, totalRoundsPerDay); // usa TU variable real
                tableFlow?.ComputeBetsForThisRound();

                if (autoAdvanceAfterBets)
                    StartCoroutine(AutoAdvanceFromBets());
                break;

            case BlackjackPhase.Reparto:
                // no mates todas las coroutines del juego
                if (dealCheckCoroutine != null)
                    StopCoroutine(dealCheckCoroutine);

                dealCheckCoroutine = StartCoroutine(WaitForInitialDealComplete());
                break;

            case BlackjackPhase.TurnoJugadores:
                tableFlow?.StartNPCDecisionTurn(); // si lo usas
                break;

            case BlackjackPhase.RevelarSegundaCarta:
                if (autoSkipRevealDealerSecondCard)
                    StartCoroutine(AutoAdvanceFromRevealSecondCard());
                break;

            case BlackjackPhase.TurnoDealer:
                (tableFlow as BlackjackTable)?.StartDealerTurnXR();
                break;

            case BlackjackPhase.Resultados:
                // 1) Resolver pagos
                tableFlow?.ResolveRoundPayouts();

                // 2) Esperar a que el jugador recoja cartas
                if (waitClearRoutine != null) StopCoroutine(waitClearRoutine);
                waitClearRoutine = StartCoroutine(WaitForTableClearThenNextRound());
                break;




        }
    }


    private System.Collections.IEnumerator AutoAdvanceFromBets()
    {
        // Espera 1 frame para que se reflejen los textos
        yield return null;

        // Solo avanza si seguimos en Apuestas
        if (currentPhase == BlackjackPhase.Apuestas)
            GoToPhase(BlackjackPhase.Reparto);
    }

    private System.Collections.IEnumerator WaitForInitialDealComplete()
    {
        // Espera a que el jugador rellene las 4 manos con 2 cartas cada una
        while (currentPhase == BlackjackPhase.Reparto)
        {
            // Necesitamos acceder al BlackjackTable real:
            var table = blackjackTableComponent as BlackjackTable; // o BlackjackTableXR si es el tuyo
            if (table != null && table.AreInitialHandsComplete(2))
            {
                GoToPhase(BlackjackPhase.TurnoJugadores);
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private System.Collections.IEnumerator AutoAdvanceFromRevealSecondCard()
    {
        // Pequeno delay para que se actualice la UI y quede claro que hubo fase
        if (autoSkipRevealDelay > 0f)
            yield return new WaitForSeconds(autoSkipRevealDelay);

        // Aqui dejaremos el hook para el futuro:
        // Cuando implementes la fase, pondras autoSkipRevealDealerSecondCard = false
        // y llamaras a tableFlow?.RevealDealerSecondCardInternal() desde un boton.
        GoToPhase(BlackjackPhase.TurnoDealer);
    }

    public void OnDealerTurnCompleted()
    {
        if (currentPhase != BlackjackPhase.TurnoDealer) return;
        GoToPhase(BlackjackPhase.Resultados);
    }

    private System.Collections.IEnumerator WaitForTableClearThenNextRound()
    {
        // Evita que por algun bug la mesa se considere vacia en el primer frame
        yield return new WaitForSeconds(minResultsTimeBeforeAutoNext);

        while (currentPhase == BlackjackPhase.Resultados)
        {
            // Necesitamos BlackjackTable real para saber si esta vacia
            BlackjackTable table = tableFlow as BlackjackTable;

            if (table != null && table.AreAllZonesEmpty())
            {
                AdvanceToNextRoundFromResults();
                yield break;
            }

            yield return new WaitForSeconds(pollClearTableInterval);
        }
    }

    private void AdvanceToNextRoundFromResults()
    {
        // 1) Incrementar ronda
        currentRound++;

        // Si tienes fin de dia / max rondas, aqui es donde lo compruebas.
        // Ejemplo:
        // if (currentRound > totalRounds) { EndDayOrGame(); return; }

        // 2) Resetear mesa para nueva ronda
        tableFlow?.ResetForNewRound();


        // 3) Volver a Apuestas
        GoToPhase(BlackjackPhase.Apuestas);
    }


    // ----------------------------------------------------------------------
    // Métodos que llamará EL JUGADOR (botones/acciones)
    // ----------------------------------------------------------------------

    /// <summary>Apuestas -> Reparto</summary>
    public void PlayerConfirmBets()
    {
        if (currentPhase != BlackjackPhase.Apuestas) return;
        GoToPhase(BlackjackPhase.Reparto);
    }

    /// <summary>Reparto -> TurnoJugadores</summary>
    public void PlayerConfirmInitialDealDone()
    {
        if (currentPhase != BlackjackPhase.Reparto) return;
        GoToPhase(BlackjackPhase.TurnoJugadores);
    }

    /// <summary>TurnoJugadores -> RevelarSegundaCarta</summary>
    public void PlayerEndPlayersTurn()
    {
        if (currentPhase != BlackjackPhase.TurnoJugadores) return;
        GoToPhase(BlackjackPhase.RevelarSegundaCarta);
    }

    /// <summary>RevelarSegundaCarta -> TurnoDealer (y se revela internamente la carta si corresponde)</summary>
    public void PlayerRevealDealerSecondCard()
    {
        if (currentPhase != BlackjackPhase.RevelarSegundaCarta) return;

        tableFlow?.RevealDealerSecondCardInternal();

        GoToPhase(BlackjackPhase.TurnoDealer);
    }

    /// <summary>TurnoDealer -> Resultados</summary>
    public void PlayerEndDealerTurn()
    {
        if (currentPhase != BlackjackPhase.TurnoDealer) return;
        GoToPhase(BlackjackPhase.Resultados);
    }

    /// <summary>
    /// Resultados: hace cálculos de ganancia/pérdida con MoneyManager.
    /// NO pasa de ronda automáticamente: el jugador decide cuándo.
    /// </summary>
    public void PlayerResolveResults()
    {
        if (currentPhase != BlackjackPhase.Resultados) return;

        tableFlow?.ResolveRoundPayouts();

        Debug.Log("[RondaManager] Resultados resueltos. Esperando a que el jugador pase a la siguiente ronda.");

        // Aquí puedes mostrar un mensaje en UI si quieres
        // uiManager?.ShowCustomMessage("Resultados aplicados. Limpia la mesa y pulsa 'Siguiente Ronda'.");
    }

    /// <summary>
    /// Resultados -> Siguiente ronda (cuando el jugador haya limpiado la mesa).
    /// </summary>
    public void PlayerNextRound()
    {
        if (currentPhase != BlackjackPhase.Resultados) return;

        OnRoundEnded?.Invoke(currentRound);

        StartNextRound();
    }

    // ----------------------------------------------------------------------
    // Compatibilidad: si algún código viejo llama a esto (NPCs), lo mantenemos.
    // Ahora NO auto-avanza, solo sirve para que tú uses ese evento si quieres.
    // ----------------------------------------------------------------------

    public void OnPlayersTurnCompleted()
    {
        if (currentPhase != BlackjackPhase.TurnoJugadores) return;
        GoToPhase(BlackjackPhase.RevelarSegundaCarta);

        Debug.Log("[RondaManager] OnPlayersTurnCompleted recibido (no cambia fase automáticamente).");
    }
}

/// <summary>
/// Interfaz de "flujo lógico" de mesa para que el RondaManager haga cálculos internos
/// sin controlar acciones físicas del dealer.
/// </summary>
public interface IBlackjackTableFlow
{
    void ResetForNewRound();

    // Apuestas
    void ComputeBetsForThisRound();

    // NPC turn (si automático)
    void StartNPCDecisionTurn();

    // Dealer reveal (interno, no físico)
    void RevealDealerSecondCardInternal();

    // Payouts
    void ResolveRoundPayouts();
}
