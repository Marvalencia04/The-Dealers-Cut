using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UIManager: controla menús, HUDs y textos principales del juego.
/// Se comunica con GameManager, RondaManager y MoneyManager.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ==== PANELES PRINCIPALES ====

    [Header("Panels")]
    [Tooltip("Menú principal del juego.")]
    [SerializeField] private GameObject mainMenuPanel;

    [Tooltip("HUD principal mientras se juega al Blackjack.")]
    [SerializeField] private GameObject blackjackHUDPanel;

    [Tooltip("HUD mientras se juega a la tragaperras.")]
    [SerializeField] private GameObject slotsHUDPanel;

    [Tooltip("Menú de pausa.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Pantalla de victoria final (si la usas).")]
    [SerializeField] private GameObject victoryPanel;

    [Tooltip("Pantalla de derrota (cuando no se alcanza la cuota).")]
    [SerializeField] private GameObject defeatPanel;

    // ==== TEXTOS HUD / INFO ====

    [Header("Texts (HUD / Info)")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private TextMeshProUGUI quotaText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI phaseText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private TextMeshProUGUI messageText;

    // ==== REFERENCIAS A MANAGERS (opcionales, se pueden pillar con Instance) ====

    [Header("Managers (opcionales, si no usas Singleton)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MoneyManager moneyManager;

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
        // Si no están asignados por inspector, intentamos cogerlos por Singleton
        if (gameManager == null && GameManager.Instance != null)
            gameManager = GameManager.Instance;

        if (moneyManager == null && MoneyManager.Instance != null)
            moneyManager = MoneyManager.Instance;

        // Nos suscribimos a eventos si existen
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(moneyManager.CurrentMoney); // para inicializar
        }

        // Estado inicial de paneles
        ShowOnlyPanel(mainMenuPanel);
        UpdateStateText(GameState.MainMenu);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameStateChanged -= HandleGameStateChanged;

        if (moneyManager != null)
            moneyManager.OnMoneyChanged -= HandleMoneyChanged;
    }

    // ----------------------------------------------------------------------
    //                          MANEJO EVENTOS
    // ----------------------------------------------------------------------

    private void HandleGameStateChanged(GameState newState)
    {
        UpdateStateUI(newState);
    }

    private void HandleMoneyChanged(int newMoney)
    {
        if (moneyText != null)
            moneyText.text = $"Dinero: {newMoney}";
    }

    // ----------------------------------------------------------------------
    //                          MÉTODOS PÚBLICOS (llamados por otros managers)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamado por GameManager al empezar un nuevo día.
    /// </summary>
    public void ShowDayIntro(int day, int quota)
    {
        if (dayText != null)
            dayText.text = $"Día: {day}";

        if (quotaText != null)
            quotaText.text = $"Cuota del día: {quota}";

        if (messageText != null)
            messageText.text = $"Comienza el día {day}. ¡Alcanza {quota} de dinero!";

        // Por defecto, asumimos que estarás en Blackjack al empezar el día.
        ShowOnlyPanel(blackjackHUDPanel);
    }

    /// <summary>
    /// Llamado por GameManager cuando se completa un día (cuota alcanzada).
    /// </summary>
    public void ShowDayCompleted(int day)
    {
        if (messageText != null)
            messageText.text = $"Día {day} completado. ¡Bien hecho!";

        // Aquí podrías activar un panel temporal o animación.
        // De momento, solo actualizamos el texto.
    }

    /// <summary>
    /// Llamado por GameManager cuando se pierde la partida (no se llega a la cuota).
    /// </summary>
    public void ShowDefeatScreen()
    {
        if (messageText != null)
            messageText.text = "No has alcanzado la cuota. Has sido despedido.";

        ShowOnlyPanel(defeatPanel);
    }

    /// <summary>
    /// Llamado por RondaManager al empezar una ronda.
    /// </summary>
    public void ShowRoundIntro(int currentRound, int totalRounds)
    {
        if (roundText != null)
            roundText.text = $"Ronda: {currentRound}/{totalRounds}";

        if (messageText != null)
            messageText.text = $"Ronda {currentRound} de {totalRounds}.";
    }

    /// <summary>
    /// Llamado por RondaManager cuando cambia la fase interna del Blackjack.
    /// </summary>
    public void UpdateBlackjackPhase(BlackjackPhase phase)
    {
        if (phaseText != null)
            phaseText.text = $"Fase: {phase}";

        if (messageText != null)
        {
            switch (phase)
            {
                case BlackjackPhase.Apuestas:
                    messageText.text = "Fase de apuestas: los jugadores ponen sus fichas.";
                    break;
                case BlackjackPhase.Reparto:
                    messageText.text = "Repartiendo cartas iniciales...";
                    break;
                case BlackjackPhase.TurnoJugadores:
                    messageText.text = "Turno de los jugadores: HIT / STAND.";
                    break;
                case BlackjackPhase.RevelarSegundaCarta:
                    messageText.text = "Mostrando la segunda carta del dealer.";
                    break;
                case BlackjackPhase.TurnoDealer:
                    messageText.text = "Turno del dealer: roba hasta 17+.";
                    break;
                case BlackjackPhase.Resultados:
                    messageText.text = "Mostrando resultados de la ronda.";
                    break;
                default:
                    messageText.text = "";
                    break;
            }
        }
    }

    /// <summary>
    /// Llamado por GameManager al cambiar de estado global (MainMenu, Blackjack, Slots, Pause, etc.).
    /// </summary>
    public void UpdateStateUI(GameState state)
    {
        UpdateStateText(state);

        switch (state)
        {
            case GameState.MainMenu:
                ShowOnlyPanel(mainMenuPanel);
                break;

            case GameState.PlayingBlackjack:
                ShowOnlyPanel(blackjackHUDPanel);
                break;

            case GameState.PlayingSlots:
                ShowOnlyPanel(slotsHUDPanel);
                break;

            case GameState.Pause:
                // Solo mostramos el panel de pausa, ocultando el resto
                ShowOnlyPanel(pausePanel);
                break;

            case GameState.Victory:
                ShowOnlyPanel(victoryPanel);
                break;

            case GameState.Defeat:
                ShowOnlyPanel(defeatPanel);
                break;

            default:
                ShowOnlyPanel(blackjackHUDPanel);
                break;
        }
    }

    // ----------------------------------------------------------------------
    //                          BOTONES / INPUT UI
    // ----------------------------------------------------------------------

    /// <summary>
    /// Botón en menú principal para empezar la partida.
    /// </summary>
    public void OnClick_StartGame()
    {
        if (gameManager != null)
            gameManager.StartGame();
    }

    /// <summary>
    /// Botón para pausar / reanudar.
    /// </summary>
    public void OnClick_TogglePause()
    {
        if (gameManager != null)
            gameManager.TogglePause();
    }

    /// <summary>
    /// Botón para reintentar tras derrota (simple: recarga la escena actual).
    /// </summary>
    public void OnClick_RestartGame()
    {
        // Aquí puedes implementar como quieras (SceneManager, etc).
        // De momento solo logueamos.
        Debug.Log("[UIManager] OnClick_RestartGame llamado (implementa recarga de escena aquí).");
    }

    /// <summary>
    /// Botón para salir del juego.
    /// </summary>
    public void OnClick_QuitGame()
    {
        Debug.Log("[UIManager] Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ----------------------------------------------------------------------
    //                          UTILIDADES INTERNAS
    // ----------------------------------------------------------------------

    private void ShowOnlyPanel(GameObject panelToShow)
    {
        // Desactivar todos
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (blackjackHUDPanel != null) blackjackHUDPanel.SetActive(false);
        if (slotsHUDPanel != null) slotsHUDPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // Activar solo el seleccionado
        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    private void UpdateStateText(GameState state)
    {
        if (stateText != null)
            stateText.text = $"Estado: {state}";
    }
}
