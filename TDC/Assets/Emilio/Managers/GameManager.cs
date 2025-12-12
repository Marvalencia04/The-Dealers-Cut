using UnityEngine;
using System;

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
/// GameManager: controla días, cuotas, estados y flujo general.
/// Haz que exista solo uno en la escena (singleton).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ==== CONFIGURACIÓN GENERAL ====

    [Header("Configuración de partida")]
    [Tooltip("Día actual de la partida (empieza en 1).")]
    [SerializeField] private int currentDay = 1;

    [Tooltip("Cuota de dinero que hay que alcanzar en el día actual.")]
    [SerializeField] private int currentQuota = 0;

    [Tooltip("Número de rondas de blackjack por día.")]
    [SerializeField] private int roundsPerDay = 5;

    [Tooltip("Cuota base del Día 1. A partir de aquí puedes escalar por día.")]
    [SerializeField] private int baseQuotaDay1 = 1000;

    [Tooltip("Curva opcional para calcular la cuota según el día (si está vacía, se usa fórmula simple). X = día, Y = cuota.")]
    [SerializeField] private AnimationCurve quotaByDayCurve;

    [Header("Opciones de inicio")]
    [Tooltip("Si está marcado, el juego se inicia automáticamente al cargar la escena.")]
    [SerializeField] private bool autoStartGameOnPlay = true;

    // ==== ESTADO ACTUAL ====

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    public int CurrentDay => currentDay;
    public int CurrentQuota => currentQuota;

    // ==== REFERENCIAS A OTROS SISTEMAS ====

    [Header("Referencias (asignar en el Inspector)")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private RondaManager rondaManager;
    //[SerializeField] private SlotsManager slotsManager; // opcional si aún no lo tienes
    [SerializeField] private UIManager uiManager;       // UI principal (HUD, menús, etc.)

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
        if (autoStartGameOnPlay)
        {
            StartGame();
        }
        else
        {
            SetGameState(GameState.MainMenu);
        }
    }

    // ----------------------------------------------------------------------
    //                          CONTROL PRINCIPAL
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamar cuando quieras iniciar la partida desde el menú principal.
    /// </summary>
    public void StartGame()
    {
        // Sin carga de partida: empezamos siempre desde cero.
        currentDay = 1;
        currentQuota = GetQuotaForDay(currentDay);

        if (moneyManager != null)
        {
            moneyManager.SetMoney(0);
        }

        SetGameState(GameState.PlayingBlackjack);
        StartNewDay();
    }

    /// <summary>
    /// Inicia un nuevo día: calcula cuota, prepara rondas y avisa a la UI.
    /// </summary>
    public void StartNewDay()
    {
        if (moneyManager == null || rondaManager == null)
        {
            Debug.LogError("[GameManager] Falta asignar MoneyManager o RondaManager en el Inspector.");
            return;
        }

        currentQuota = GetQuotaForDay(currentDay);

        Debug.Log($"[GameManager] Comienza el día {currentDay}. Cuota: {currentQuota}");

        // Avisar a la UI (si existe)
        if (uiManager != null)
        {
            uiManager.ShowDayIntro(currentDay, currentQuota);
        }

        // Configurar rondas del día
        rondaManager.SetupForNewDay(roundsPerDay);

        // Empezar primera ronda
        rondaManager.StartFirstRound();
    }

    /// <summary>
    /// Llamado por el RondaManager cuando se han jugado todas las rondas del día.
    /// </summary>
    public void OnAllRoundsFinished()
    {
        if (moneyManager == null)
        {
            Debug.LogError("[GameManager] MoneyManager no asignado.");
            return;
        }

        int money = moneyManager.CurrentMoney;
        Debug.Log($"[GameManager] Fin del día {currentDay}. Dinero actual: {money}. Cuota: {currentQuota}");

        if (money >= currentQuota)
        {
            // Día superado
            DayCompleted();
        }
        else
        {
            // Derrota
            DayFailed();
        }
    }

    /// <summary>
    /// Llamado cuando el jugador ha superado la cuota del día.
    /// </summary>
    private void DayCompleted()
    {
        Debug.Log($"[GameManager] Día {currentDay} COMPLETADO.");

        if (uiManager != null)
        {
            uiManager.ShowDayCompleted(currentDay);
        }

        // A partir del Día 2 la tragaperras está disponible.
        //bool slotsAvailable = (slotsManager != null && currentDay >= 2);
        bool slotsAvailable = ( currentDay >= 2);
        if (slotsAvailable)
        {
            SetGameState(GameState.PlayingSlots);
            //slotsManager.EnableSlots(true, currentDay); // método sugerido
        }
        else
        {
            // Si aún no hay tragaperras, pasamos directamente al siguiente día.
            GoToNextDay();
        }
    }

    /// <summary>
    /// Llamado cuando el jugador NO ha llegado a la cuota.
    /// </summary>
    private void DayFailed()
    {
        Debug.Log($"[GameManager] Día {currentDay} FRACASADO. Partida terminada.");

        SetGameState(GameState.Defeat);

        if (uiManager != null)
        {
            uiManager.ShowDefeatScreen();
        }

        // No hay guardado, simplemente se queda en estado de derrota.
    }

    /// <summary>
    /// Llamar desde SlotsManager cuando el jugador termine con la tragaperras
    /// y quiera pasar al siguiente día.
    /// </summary>
    public void OnSlotsFinished()
    {
        Debug.Log("[GameManager] Slots terminadas. Pasando al siguiente día.");
        GoToNextDay();
    }

    /// <summary>
    /// Incrementa el día y comienza un nuevo ciclo de Blackjack.
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
        Debug.Log($"[GameManager] Estado cambiado a: {CurrentState}");

        // Avisar a otros sistemas
        OnGameStateChanged?.Invoke(CurrentState);

        // UI
        if (uiManager != null)
        {
            uiManager.UpdateStateUI(CurrentState);
        }

        // Comportamientos típicos según estado
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
            // Volver al estado de juego anterior (aquí simplificamos y lo ponemos en Blackjack)
            SetGameState(GameState.PlayingBlackjack);
        }
        else
        {
            SetGameState(GameState.Pause);
        }
    }

    // ----------------------------------------------------------------------
    //                          CUOTAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Calcula la cuota necesaria para un día concreto.
    /// Puedes modificar esta lógica más adelante.
    /// </summary>
    private int GetQuotaForDay(int day)
    {
        if (quotaByDayCurve != null && quotaByDayCurve.length > 0)
        {
            float value = quotaByDayCurve.Evaluate(day);
            int quotaFromCurve = Mathf.Max(0, Mathf.RoundToInt(value));
            if (quotaFromCurve > 0)
                return quotaFromCurve;
        }

        // Fórmula simple: cuota base * día (ej: Día 1 -> 1000, Día 2 -> 2000...)
        return baseQuotaDay1 * day;
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
