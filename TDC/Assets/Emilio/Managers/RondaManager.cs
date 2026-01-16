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

    [Header("Rondas por d�a")]
    [SerializeField] private int defaultRoundsPerDay = 5;
    [Header("Collider que solo se activa en Resultados")]
    [SerializeField] private Collider colliderSoloEnResultados;
    [SerializeField] private bool desactivarEnAwake = true;


    [Header("Referencias")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private TrampasManager trampasManager;

    [Tooltip("Gate que habilita/bloquea interacciones VR seg�n fase (DeckXR, recoger, zonas...).")]
    [SerializeField] private BlackjackInteractionGateXR interactionGate;

    [Tooltip("Mesa XR o controlador de blackjack que hace c�lculos internos (apuestas, resultados, revelar carta...).")]
    [SerializeField] private MonoBehaviour blackjackTableComponent;

    // Si tu mesa implementa estas funciones (las que venimos usando)
    private IBlackjackTableFlow tableFlow;

    // Estado de d�a
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

    [Header("End of Day / Teleport")]
    [SerializeField] private Transform endOfDayTeleportTarget;
    [SerializeField] private Transform playerRoot;



    [SerializeField] private MoneyManager moneyManager;



    public BlackjackPhase CurrentPhase => currentPhase;

    public int CurrentRound => currentRound;
    public int TotalRoundsPerDay => totalRoundsPerDay;

    public event Action<BlackjackPhase> OnPhaseChanged;
    public event Action<int> OnRoundStarted;
    public event Action<int> OnRoundEnded;
    private Coroutine dealCheckCoroutine;

    //Para que respawneen NPCs
    [SerializeField] private RoundObjectRotator roundObjectRotator;


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
            //Debug.LogWarning("[RondaManager] blackjackTableComponent no implementa IBlackjackTableFlow. " +
                            // "Podr�s cambiar fases, pero no ejecutar c�lculos de apuestas/resultados.");
        }
        if (colliderSoloEnResultados != null && desactivarEnAwake)
                colliderSoloEnResultados.enabled = false;

    }

    // ----------------------------------------------------------------------
    // Configuraci�n por d�a (llamado por GameManager)
    // ----------------------------------------------------------------------

    public void SetupForNewDay(int rounds)
    {
        totalRoundsPerDay = rounds > 0 ? rounds : defaultRoundsPerDay;
        currentRound = 0;
        currentPhase = BlackjackPhase.None;

        //Debug.Log($"[RondaManager] Nuevo d�a configurado. Rondas: {totalRoundsPerDay}");
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
            //Debug.Log("[RondaManager] D�a terminado: todas las rondas completadas.");
            gameManager?.OnAllRoundsFinished();
            return;
        }

        //Debug.Log($"[RondaManager] Ronda {currentRound}/{totalRoundsPerDay} iniciada.");

        // Reseteo interno de la mesa (cartas, estados, etc.)
        tableFlow?.ResetForNewRound();

        // UI
        uiManager?.ShowRoundIntro(currentRound, totalRoundsPerDay);

        OnRoundStarted?.Invoke(currentRound);
        
        // Entramos en apuestas (c�lculo interno + permisos)
        GoToPhase(BlackjackPhase.Apuestas);
    }

    // ----------------------------------------------------------------------
    // Cambio de fase (solo aqu� se cambia)
    // ----------------------------------------------------------------------

    public void GoToPhase(BlackjackPhase newPhase)
    {
        currentPhase = newPhase;

        uiManager?.UpdateBlackjackPhase(currentPhase);
        trampasManager?.OnBlackjackPhaseChanged(currentPhase);
        interactionGate?.ApplyPhase(currentPhase);
        if (colliderSoloEnResultados != null && desactivarEnAwake)
            colliderSoloEnResultados.enabled = false;

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
                //Esto es para lo de los NPCs
                if (roundObjectRotator != null)
                    Debug.Log("aaaaaaaaa (pero dentro de RondaManager)");
                    roundObjectRotator.OnRoundStarted();
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
                moneyManager.NotifyRoundEnd();
                

                if (IsLastRoundOfDay())
                {
                    if (colliderSoloEnResultados != null)
                        colliderSoloEnResultados.enabled = true;

                    //Debug.Log("aaaaaaaaaaaaaaaaa");
                    StopAllCoroutines();

                    // (Opcional) Bloquear recoger cartas / interacciones si quieres
                    // interactionGate?.ApplyPhase(BlackjackPhase.Resultados); // si esto habilita recoger, mejor no llamarlo o a�ade un modo "ResultsLocked"
                    // Si ya est�s en Resultados y el gate pone Collect=true, puedes desactivar collectors manualmente (te lo pongo abajo).

                    if (gameManager != null)
                        gameManager.OnDayFinished(); // aqui se decide derrota/victoria segun cuota

                    // Importante: NO arrancar la espera de limpiar mesa
                    //return;
                }

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
        //Hola, soy Adri, Emilio me ha dicho que pegue esto aquí
        //Es de una función que hay pa'bajo
        // 1) Detener coroutines de espera (reparto/resultados/etc.)
        StopAllCoroutines();

        // 2) (Opcional pero recomendado) Resetea mesa logica para evitar estados raros
        // Si tu BlackjackTable tiene ResetForNewRound/ResetTableForNewRound usa el que tengas:
        if (tableFlow != null)
            tableFlow.ResetForNewRound();

        // 1) Incrementar ronda
        currentRound++;
        Debug.Log(currentRound);

        if (currentRound > totalRoundsPerDay)
        {
            EndDayAndTeleport();
            return;
        }
        // 2) Resetear mesa para nueva ronda
        tableFlow?.ResetForNewRound();


        // 3) Volver a Apuestas
        GoToPhase(BlackjackPhase.Apuestas);
    }

    private void EndDayAndTeleport()
    {
        //Debug.Log("[RondaManager] D�a completado. Teletransportando jugador.");

        if (playerRoot != null && endOfDayTeleportTarget != null)
        {
            playerRoot.position = endOfDayTeleportTarget.position;
            playerRoot.rotation = endOfDayTeleportTarget.rotation * Quaternion.Euler(0f, 180f, 0f);
            
        }

        else
        {
            //Debug.LogWarning("[RondaManager] Falta asignar playerRoot o endOfDayTeleportTarget.");
        }
    }

    private bool IsLastRoundOfDay()
    {
        // Si tu dia tiene totalRoundsPerDay, esto es lo normal:
        return currentRound >= totalRoundsPerDay;
    }


    // ----------------------------------------------------------------------
    // DEBUG: avanzar ronda con tecla L
    // ----------------------------------------------------------------------

    [SerializeField] private bool enableDebugAdvanceRoundWithL = true;

    private void Update()
    {
        if (!enableDebugAdvanceRoundWithL) return;

        if (Input.GetKeyDown(KeyCode.L))
        {
            //Debug.Log("[RondaManager] DEBUG: Tecla L pulsada -> avanzar ronda.");
            DebugAdvanceRound();
        }
    }

    /// <summary>
    /// Fuerza el avance a la siguiente ronda como si se hubieran recogido cartas.
    /// Usa esto solo para debug.
    /// </summary>
    private void DebugAdvanceRound()
    {
        // 1) Detener coroutines de espera (reparto/resultados/etc.)
        StopAllCoroutines();

        // 2) (Opcional pero recomendado) Resetea mesa logica para evitar estados raros
        // Si tu BlackjackTable tiene ResetForNewRound/ResetTableForNewRound usa el que tengas:
        if (tableFlow != null)
            tableFlow.ResetForNewRound();

        // 3) Incrementar ronda y comprobar fin de dia
        currentRound++;

        // IMPORTANTE: usa tu variable real del total (por ejemplo totalRoundsPerDay / roundsPerDay)
        if (currentRound > totalRoundsPerDay)
        {
            // Llama a tu funcion de fin de dia/teleport
            EndDayAndTeleport();
            return;
        }

        // 4) Volver a Apuestas (esto ya debe actualizar UI y generar apuestas si lo tienes asi)
        GoToPhase(BlackjackPhase.Apuestas);
    }

    public void ResetForNewDay()
    {
        // parar esperas de reparto/resultados/etc.
        StopAllCoroutines();

        // Reiniciar contador de rondas
        currentRound = 1;

        // Reset mesa
        if (tableFlow != null)
            tableFlow.ResetForNewRound();

        // Volver a fase apuestas (tu GoToPhase ya actualiza UI y genera apuestas)
        GoToPhase(BlackjackPhase.Apuestas);

        // UI de ronda (ajusta el nombre de tu variable total rounds)
        if (uiManager != null)
            uiManager.ShowRoundIntro(currentRound, totalRoundsPerDay);

        //Debug.Log("[RondaManager] ResetForNewDay -> currentRound=1 y vuelta a Apuestas");
    }



    // ----------------------------------------------------------------------
    // M�todos que llamar� EL JUGADOR (botones/acciones)
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
    /// Resultados: hace c�lculos de ganancia/p�rdida con MoneyManager.
    /// NO pasa de ronda autom�ticamente: el jugador decide cu�ndo.
    /// </summary>
    public void PlayerResolveResults()
    {
        if (currentPhase != BlackjackPhase.Resultados) return;

        tableFlow?.ResolveRoundPayouts();

        //Debug.Log("[RondaManager] Resultados resueltos. Esperando a que el jugador pase a la siguiente ronda.");

        // Aqu� puedes mostrar un mensaje en UI si quieres
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
    // Compatibilidad: si alg�n c�digo viejo llama a esto (NPCs), lo mantenemos.
    // Ahora NO auto-avanza, solo sirve para que t� uses ese evento si quieres.
    // ----------------------------------------------------------------------

    public void OnPlayersTurnCompleted()
    {
        if (currentPhase != BlackjackPhase.TurnoJugadores) return;
        GoToPhase(BlackjackPhase.RevelarSegundaCarta);

        //Debug.Log("[RondaManager] OnPlayersTurnCompleted recibido (no cambia fase autom�ticamente).");
    }
}

/// <summary>
/// Interfaz de "flujo l�gico" de mesa para que el RondaManager haga c�lculos internos
/// sin controlar acciones f�sicas del dealer.
/// </summary>
public interface IBlackjackTableFlow
{
    void ResetForNewRound();

    // Apuestas
    void ComputeBetsForThisRound();

    // NPC turn (si autom�tico)
    void StartNPCDecisionTurn();

    // Dealer reveal (interno, no f�sico)
    void RevealDealerSecondCardInternal();

    // Payouts
    void ResolveRoundPayouts();
}
