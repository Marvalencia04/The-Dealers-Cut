using UnityEngine;
using System;
using UnityEngine.LowLevel;

/// <summary>
/// Estados globales del juego.
/// </summary>
public enum GameState
{
    MainMenu,
    PlayingBlackjack,
    PlayingSlots,
    Pause,
    Victory,
    Defeat
}

/// <summary>
/// GameManager: controla d�as, cuotas, estados y flujo general.
/// Haz que exista solo uno en la escena (singleton).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ==== CONFIGURACI�N GENERAL ====

    [Header("Configuraci�n de partida")]
    [Tooltip("D�a actual de la partida (empieza en 1).")]
    [SerializeField] private int currentDay = 1;

    [Tooltip("Cuota de dinero que hay que alcanzar en el d�a actual.")]
    [SerializeField] private int currentQuota = 0;

    [Tooltip("N�mero de rondas de blackjack por d�a.")]
    [SerializeField] private int roundsPerDay = 5;

    [Header("Cuotas por dia (exactamente 3)")]
    [Tooltip("Indice 0 = Dia 1, 1 = Dia 2, 2 = Dia 3")]
    [SerializeField] private int[] quotasByDay = new int[3] { 2000, 3000, 4000 };

    [Tooltip("Curva opcional para calcular la cuota seg�n el d�a (si est� vac�a, se usa f�rmula simple). X = d�a, Y = cuota.")]
    [SerializeField] private AnimationCurve quotaByDayCurve;

    [Header("End of Day / Teleport")]
    [SerializeField] private Transform endOfDayTeleportTarget;
    [SerializeField] private Transform playerRoot;

    [Header("Locomotion")]
    [SerializeField] private Behaviour locomotionScript;




    // ==== ESTADO ACTUAL ====

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int CurrentDay => currentDay;
    public int CurrentQuota => currentQuota;

    // ==== REFERENCIAS A OTROS SISTEMAS ====

    [Header("Referencias (asignar en el Inspector)")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private RondaManager rondaManager;
    //[SerializeField] private SlotsManager slotsManager; // opcional si a�n no lo tienes
    [SerializeField] private UIManager uiManager;       // UI principal (HUD, men�s, etc.)

    // ==== EVENTOS OPCIONALES ====

    /// <summary>
    /// Evento llamado cuando cambia el estado del juego.
    /// Otros scripts pueden suscribirse si lo necesitan.
    /// </summary>
    public event Action<GameState> OnGameStateChanged;

    // ----------------------------------------------------------------------
    //                          CICLO DE VIDA
    // ----------------------------------------------------------------------

    private void Awake()
    {
        // Singleton cl�sico
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
        SetGameState(GameState.MainMenu);

        if (rondaManager == null) rondaManager = RondaManager.Instance;
        if (uiManager == null) uiManager = UIManager.Instance;

        // Inicializar cuota del dia actual
        currentQuota = GetQuotaForDay(currentDay);

        if (uiManager != null)
            uiManager.ShowDayIntro(currentDay, currentQuota);
    }

    // ----------------------------------------------------------------------
    //                          CONTROL PRINCIPAL
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamar cuando quieras iniciar la partida desde el men� principal.
    /// </summary>
    public void StartGame()
    {
        // Sin carga de partida: empezamos siempre desde cero.
        currentDay = 1;
        currentQuota = GetQuotaForDay(currentDay);

        if (moneyManager != null)
        {
            moneyManager.SetMoney(1000);
        }

        SetGameState(GameState.PlayingBlackjack);
        StartNewDay();
    }

    /// <summary>
    /// Inicia un nuevo d�a: calcula cuota, prepara rondas y avisa a la UI.
    /// </summary>
    public void StartNewDay()
    {
        if (moneyManager == null || rondaManager == null)
        {
            //Debug.LogError("[GameManager] Falta asignar MoneyManager o RondaManager en el Inspector.");
            return;
        }

        currentQuota = GetQuotaForDay(currentDay);

        //Debug.Log($"[GameManager] Comienza el d�a {currentDay}. Cuota: {currentQuota}");

        // Avisar a la UI (si existe)
        if (uiManager != null)
        {
            uiManager.ShowDayIntro(currentDay, currentQuota);
        }

        // Configurar rondas del d�a
        rondaManager.SetupForNewDay(roundsPerDay);

        // Empezar primera ronda
        rondaManager.StartFirstRound();
    }

    /// <summary>
    /// Llamado por el RondaManager cuando se han jugado todas las rondas del d�a.
    /// </summary>
    public void OnAllRoundsFinished()
    {
        if (moneyManager == null)
        {
            //Debug.LogError("[GameManager] MoneyManager no asignado.");
            return;
        }

        int money = moneyManager.CurrentMoney;
        //Debug.Log($"[GameManager] Fin del d�a {currentDay}. Dinero actual: {money}. Cuota: {currentQuota}");

        if (money >= currentQuota)
        {
            // D�a superado
            DayCompleted();
        }
        else
        {
            // Derrota
            DayFailed();
        }
    }

    /// <summary>
    /// Llamado cuando el jugador ha superado la cuota del d�a.
    /// </summary>
    private void DayCompleted()
    {
        //Debug.Log($"[GameManager] D�a {currentDay} COMPLETADO.");

        if (uiManager != null)
        {
            uiManager.ShowDayCompleted(currentDay);
        }

        // A partir del D�a 2 la tragaperras est� disponible.
        //bool slotsAvailable = (slotsManager != null && currentDay >= 2);
        bool slotsAvailable = ( currentDay >= 2);
        if (slotsAvailable)
        {
            SetGameState(GameState.PlayingSlots);
            //slotsManager.EnableSlots(true, currentDay); // m�todo sugerido
        }
        else
        {
            // Si a�n no hay tragaperras, pasamos directamente al siguiente d�a.
            GoToNextDay();
        }
    }

    /// <summary>
    /// Llamado cuando el jugador NO ha llegado a la cuota.
    /// </summary>
    private void DayFailed()
    {
        //Debug.Log($"[GameManager] D�a {currentDay} FRACASADO. Partida terminada.");

        SetGameState(GameState.Defeat);

        if (uiManager != null)
        {
            uiManager.ShowDefeatScreen();
        }

        // No hay guardado, simplemente se queda en estado de derrota.
    }

    /// <summary>
    /// Llamar desde SlotsManager cuando el jugador termine con la tragaperras
    /// y quiera pasar al siguiente d�a.
    /// </summary>
    public void OnSlotsFinished()
    {
        //Debug.Log("[GameManager] Slots terminadas. Pasando al siguiente d�a.");
        GoToNextDay();
    }

    /// <summary>
    /// Incrementa el d�a y comienza un nuevo ciclo de Blackjack.
    /// </summary>
    private void GoToNextDay()
    {
        currentDay++;
        SetGameState(GameState.PlayingBlackjack);
        StartNewDay();
    }

    // ----------------------------------------------------------------------
    //                          ESTADOS
    // ----------------------------------------------------------------------

    public void SetGameState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        //Debug.Log($"[GameManager] Estado cambiado a: {CurrentState}");

        // Avisar a otros sistemas
        OnGameStateChanged?.Invoke(CurrentState);

        // UI
        if (uiManager != null)
        {
            uiManager.UpdateStateUI(CurrentState);
        }

        // Comportamientos t�picos seg�n estado
        switch (CurrentState)
        {
            case GameState.Pause:
                Time.timeScale = 0f;
                break;

            default:
                Time.timeScale = 1f;
                break;
        }
    }

    public void TogglePause()
    {
        if (CurrentState == GameState.Pause)
        {
            // Volver al estado de juego anterior (aqu� simplificamos y lo ponemos en Blackjack)
            SetGameState(GameState.PlayingBlackjack);
        }
        else
        {
            SetGameState(GameState.Pause);
        }
    }

    public void AdvanceToNextDay()
    {
        //Debug.Log("[GameManager] Avanzado al Dia " + currentDay + " | Cuota: " + currentQuota);

        // Si ya estabas en el dia 3 y se intenta "pasar", victoria
        if (currentDay >= 3)
        {
            TriggerVictory();
            return;
        }

        // Avanzar dia
        currentDay++;

        // Si por cualquier razon se pasara de 3, victoria
        if (currentDay > 3)
        {
            TriggerVictory();
            return;
        }

        // Actualizar cuota del nuevo dia
        currentQuota = GetQuotaForDay(currentDay);

        // Reset rondas + volver a apuestas
        if (rondaManager != null)
            rondaManager.ResetForNewDay();

        // UI de dia
        if (uiManager != null)
            uiManager.ShowDayIntro(currentDay, currentQuota);

        
    }

    public void TeleportMesaBJ()
    {
        if (playerRoot != null && endOfDayTeleportTarget != null)
        {
            playerRoot.position = endOfDayTeleportTarget.position;
            playerRoot.rotation = endOfDayTeleportTarget.rotation * Quaternion.Euler(0f, -90f, 0f);
            locomotionScript.enabled = false;
        }

        else
        {
            //Debug.LogWarning("[RondaManager] Falta asignar playerRoot o endOfDayTeleportTarget.");
        }
    }

    private void TriggerVictory()
    {
        //Debug.Log("[GameManager] Victoria! Fin del Dia 3.");

        // Parar el juego
        Time.timeScale = 0f;

        // Mostrar pantalla de victoria
        // Asumo que tu UIManager ya maneja GameState.Victory y ense�a victoryPanel
        if (uiManager != null)
            uiManager.UpdateStateUI(GameState.Victory);

        // Si tienes eventos o estados:
        // SetState(GameState.Victory);
    }

    public void OnDayFinished()
    {
        int money = (MoneyManager.Instance != null) ? MoneyManager.Instance.CurrentMoney : 0;
        int quota = CurrentQuota;

        //Debug.Log("[GameManager] Fin de dia " + currentDay + " | Dinero=" + money + " | Cuota=" + quota);

        if (money < quota)
        {
            TriggerDefeat();
            return;
        }

        // Dia superado (si es el dia 3, victoria final; si no, te quedas en el "hub" para avanzar)
        if (currentDay >= 3)
        {
            TriggerVictory();
        }
        else
        {
            // Aqui puedes mostrar un panel/mensaje de "Dia completado"
            // y dejar al jugador pulsar el boton "Siguiente dia".
            if (uiManager != null)
                uiManager.ShowDayCompleted(currentDay);
        }
    }

    private void TriggerDefeat()
    {
        //Debug.Log("[GameManager] Derrota! No se alcanzo la cuota del dia.");

        Time.timeScale = 0f;

        if (uiManager != null)
            uiManager.UpdateStateUI(GameState.Defeat);
    }



    public int GetQuotaForDay(int day)
    {
        // day 1..3
        day = Mathf.Clamp(day, 1, 3);

        if (quotasByDay == null || quotasByDay.Length < 3)
        {
            //Debug.LogWarning("[GameManager] quotasByDay no esta bien configurado. Usando fallback 2000/3000/4000.");
            int[] fallback = { 2000, 3000, 4000 };
            return fallback[day - 1];
        }

        return Mathf.Max(0, quotasByDay[day - 1]);
    }

    // ----------------------------------------------------------------------
    //                          UTILIDADES
    // ----------------------------------------------------------------------

#if UNITY_EDITOR
    // Esto es solo para depurar desde el editor (no se compila en build).
    [ContextMenu("Debug/Simular Fin de Rondas")]
    private void Debug_SimulateEndOfRounds()
    {
        OnAllRoundsFinished();
    }

    [ContextMenu("Debug/Simular Slots Terminadas")]
    private void Debug_SimulateSlotsFinished()
    {
        OnSlotsFinished();
    }
#endif
}
