using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Fases internas de una ronda de Blackjack.
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

/// <summary>
/// RondaManager: controla el número de rondas por día
/// y el flujo de fases de cada ronda de Blackjack.
/// </summary>
public class RondaManager : MonoBehaviour
{
    public static RondaManager Instance { get; private set; }

    // ==== CONFIGURACIÓN GENERAL ====

    [Header("Rondas por día")]
    [Tooltip("Número de rondas de Blackjack a jugar cada día (se puede sobrescribir desde GameManager).")]
    [SerializeField] private int defaultRoundsPerDay = 5;

    // ==== ESTADO ACTUAL ====

    [SerializeField] private int totalRoundsPerDay;
    [SerializeField] private int currentRound;

    [SerializeField] private BlackjackPhase currentPhase = BlackjackPhase.None;
    public int CurrentRound => currentRound;
    public int TotalRoundsPerDay => totalRoundsPerDay;
    public BlackjackPhase CurrentPhase => currentPhase;

    // ==== REFERENCIAS A OTROS SISTEMAS ====

    [Header("Referencias (asignar en el Inspector)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MoneyManager moneyManager;
    //[SerializeField] private BlackjackTable blackjackTable;  // ESTE LO IMPLEMENTAS TÚ
    [SerializeField] private TrampasManager trampasManager;  // Opcional
    [SerializeField] private UIManager uiManager;            // Opcional HUD / textos

    // ==== EVENTOS ====

    /// <summary>
    /// Evento llamado cuando cambia la fase de la ronda.
    /// Útil para UI, trampas, etc.
    /// </summary>
    public event Action<BlackjackPhase> OnPhaseChanged;

    /// <summary>
    /// Evento llamado cuando empieza una nueva ronda.
    /// </summary>
    public event Action<int> OnRoundStarted;

    /// <summary>
    /// Evento llamado cuando termina una ronda.
    /// </summary>
    public event Action<int> OnRoundEnded;

    // ----------------------------------------------------------------------
    //                          CICLO DE VIDA
    // ----------------------------------------------------------------------

    private void Awake()
    {
        // Singleton clásico
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (totalRoundsPerDay <= 0)
        {
            totalRoundsPerDay = defaultRoundsPerDay;
        }
    }

    // ----------------------------------------------------------------------
    //                          CONFIGURACIÓN POR DÍA
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamado por GameManager al empezar un día nuevo.
    /// </summary>
    public void SetupForNewDay(int rounds)
    {
        totalRoundsPerDay = rounds > 0 ? rounds : defaultRoundsPerDay;
        currentRound = 0;
        currentPhase = BlackjackPhase.None;

        Debug.Log($"[RondaManager] Setup nuevo día. Rondas totales: {totalRoundsPerDay}");
    }

    /// <summary>
    /// Llamado por GameManager para arrancar la primera ronda del día.
    /// </summary>
    public void StartFirstRound()
    {
        StartNextRound();
    }

    // ----------------------------------------------------------------------
    //                          CONTROL DE RONDAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Comienza la siguiente ronda o avisa al GameManager de que se han terminado todas.
    /// </summary>
    public void StartNextRound()
    {
        currentRound++;

        if (currentRound > totalRoundsPerDay)
        {
            Debug.Log("[RondaManager] Todas las rondas del día han terminado.");
            // Avisamos al GameManager para que evalúe la cuota y decida qué hacer.
            if (gameManager != null)
            {
                gameManager.OnAllRoundsFinished();
            }
            else
            {
                Debug.LogError("[RondaManager] GameManager no asignado.");
            }
            return;
        }

        Debug.Log($"[RondaManager] Comienza ronda {currentRound}/{totalRoundsPerDay}");

        /*// Reseteamos la mesa de Blackjack para una nueva ronda.
        if (blackjackTable != null)
        {
            blackjackTable.ResetTableForNewRound();
        }
        else
        {
            Debug.LogWarning("[RondaManager] BlackjackTable no asignado. No se puede gestionar la lógica de cartas.");
        }*/

        // Avisar a otros sistemas (UI, etc.)
        OnRoundStarted?.Invoke(currentRound);
        if (uiManager != null)
        {
            uiManager.ShowRoundIntro(currentRound, totalRoundsPerDay);
        }

        // Primera fase: apuestas
        GoToPhase(BlackjackPhase.Apuestas);
    }

    /// <summary>
    /// Termina la ronda actual y pasa a la siguiente.
    /// </summary>
    private void EndCurrentRound()
    {
        Debug.Log($"[RondaManager] Ronda {currentRound} terminada.");

        OnRoundEnded?.Invoke(currentRound);

        // Pequeña transición / delay si quieres (o menú de siguiente ronda).
        StartNextRound();
    }

    // ----------------------------------------------------------------------
    //                          CONTROL DE FASES
    // ----------------------------------------------------------------------

    /// <summary>
    /// Cambia la fase actual y ejecuta la lógica de entrada de la nueva fase.
    /// </summary>
    public void GoToPhase(BlackjackPhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"[RondaManager] Fase cambiada a: {currentPhase}");

        // Avisar a listeners externos
        OnPhaseChanged?.Invoke(currentPhase);

        // Avisar al sistema de trampas para que sepa qué se puede usar ahora
        if (trampasManager != null)
        {
            trampasManager.OnBlackjackPhaseChanged(currentPhase);
        }

        // Avisar a la UI (texto con el nombre de la fase, por ejemplo)
        if (uiManager != null)
        {
            uiManager.UpdateBlackjackPhase(currentPhase);
        }

        // Lógica de entrada a fase
        switch (currentPhase)
        {
            case BlackjackPhase.Apuestas:
                StartApuestasPhase();
                break;

            case BlackjackPhase.Reparto:
                StartRepartoPhase();
                break;

            case BlackjackPhase.TurnoJugadores:
                StartTurnoJugadoresPhase();
                break;

            case BlackjackPhase.RevelarSegundaCarta:
                StartRevelarSegundaCartaPhase();
                break;

            case BlackjackPhase.TurnoDealer:
                StartTurnoDealerPhase();
                break;

            case BlackjackPhase.Resultados:
                StartResultadosPhase();
                break;
        }
    }

    // ----------------------------------------------------------------------
    //                          FASE 1: APUeSTAS
    // ----------------------------------------------------------------------

    private void StartApuestasPhase()
    {
        // Aquí los jugadores IA deciden cuánto apuestan,
        // y el jugador (croupier) ve las fichas en la mesa.

       /* if (blackjackTable != null)
        {
            blackjackTable.GenerateBetsForPlayers();   // método sugerido
        }*/

        // En VR, probablemente esperes a que el jugador haga algo (por ejemplo,
        // pulsar un botón "Repartir" o confirmar las apuestas).
        // Dejo una función pública que puedas llamar desde un botón u otro script:
        // ConfirmApuestasAndContinue();

        // Si quieres que sea automático, puedes descomentar esto:
        // ConfirmApuestasAndContinue();
    }

    /// <summary>
    /// Llamar desde la UI / input cuando el jugador quiera seguir tras las apuestas.
    /// </summary>
    public void ConfirmApuestasAndContinue()
    {
        if (currentPhase != BlackjackPhase.Apuestas) return;

        // Aquí podrías validar que todas las apuestas son válidas, etc.
        GoToPhase(BlackjackPhase.Reparto);
    }

    // ----------------------------------------------------------------------
    //                          FASE 2: REPARTO
    // ----------------------------------------------------------------------

    private void StartRepartoPhase()
    {
       /* if (blackjackTable != null)
        {
            blackjackTable.DealInitialCards();
        }*/

        // Si quieres animaciones con tiempo, podrías usar una corrutina.
        // Por ahora, pasamos directamente al turno de jugadores:
        GoToPhase(BlackjackPhase.TurnoJugadores);
    }

    // ----------------------------------------------------------------------
    //                          FASE 3: TURNO JUGADORES
    // ----------------------------------------------------------------------

    private void StartTurnoJugadoresPhase()
    {
        // Aquí empieza el HIT/STAND de cada jugador IA,
        // y también del dealer si interviene como jugador en algún sentido.

       /* if (blackjackTable != null)
        {
            blackjackTable.StartPlayersTurn();
        }*/

        // IMPORTANTE:
        // No avanzamos de fase hasta que la mesa nos avise de que
        // todos los jugadores han terminado. Para eso dejamos este método:
        // OnPlayersTurnCompleted() que blackjackTable llamará.
    }

    /// <summary>
    /// Llamar desde BlackjackTable cuando todos los jugadores han decidido HIT/STAND.
    /// </summary>
    public void OnPlayersTurnCompleted()
    {
        if (currentPhase != BlackjackPhase.TurnoJugadores) return;
        GoToPhase(BlackjackPhase.RevelarSegundaCarta);
    }

    // ----------------------------------------------------------------------
    //                          FASE 4: REVELAR 2ª CARTA DEALER
    // ----------------------------------------------------------------------

    private void StartRevelarSegundaCartaPhase()
    {
       /* if (blackjackTable != null)
        {
            blackjackTable.RevealDealerSecondCard();
        }*/

        // Se puede hacer una pequeña pausa visual si quieres (corrutina).
        GoToPhase(BlackjackPhase.TurnoDealer);
    }

    // ----------------------------------------------------------------------
    //                          FASE 5: TURNO DEALER
    // ----------------------------------------------------------------------

    private void StartTurnoDealerPhase()
    {
       /* if (blackjackTable != null)
        {
            blackjackTable.PlayDealerTurn(); // El dealer roba hasta 17+
        }*/

        // Igual que antes, puedes hacer esto sincrónico o asíncrono.
        GoToPhase(BlackjackPhase.Resultados);
    }

    // ----------------------------------------------------------------------
    //                          FASE 6: RESULTADOS
    // ----------------------------------------------------------------------

    private void StartResultadosPhase()
    {
        /*if (blackjackTable != null)
        {
            // Este método debería encargarse de:
            // - Comparar manos
            // - Llamar a MoneyManager.DealerWins / DealerPays...
            // - Aplicar penalizaciones de trampas si procede
            blackjackTable.ResolveBetsAndPayouts();
        }*/

        // Puedes mostrar los resultados un rato en pantalla antes de pasar
        // a la siguiente ronda. Aquí te dejo una corrutina simple de ejemplo.
        StartCoroutine(WaitAndEndRound(1.5f));
    }

    private IEnumerator WaitAndEndRound(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        EndCurrentRound();
    }

    // ----------------------------------------------------------------------
    //                          DEBUG / UTILIDADES
    // ----------------------------------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("Debug/Forzar siguiente fase")]
    private void Debug_ForceNextPhase()
    {
        switch (currentPhase)
        {
            case BlackjackPhase.Apuestas:
                ConfirmApuestasAndContinue();
                break;

            case BlackjackPhase.TurnoJugadores:
                OnPlayersTurnCompleted();
                break;

            default:
                Debug.LogWarning("[RondaManager] Solo se fuerza desde Apuestas o TurnoJugadores en este debug.");
                break;
        }
    }
#endif
}
