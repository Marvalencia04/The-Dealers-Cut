using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ==== PANELES PRINCIPALES ====
    // Nota: si tus panels ahora existen en ambos canvases, los metemos dentro de HandUI.
    // Si son globales (un solo panel en la escena), dejalos fuera.
    // Aqui asumo que TODO el HUD esta duplicado (uno por mano).

    [System.Serializable]
    public class HandUI
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject blackjackHUDPanel;
        public GameObject slotsHUDPanel;
        public GameObject pausePanel;
        public GameObject victoryPanel;
        public GameObject defeatPanel;

        [Header("Texts (HUD / Info)")]
        public TextMeshProUGUI dayText;
        public TextMeshProUGUI quotaText;
        public TextMeshProUGUI moneyText;
        public TextMeshProUGUI roundText;
        public TextMeshProUGUI phaseText;
        public TextMeshProUGUI stateText;
        public TextMeshProUGUI messageText;
    }

    [Header("Hand UI (0 = Left, 1 = Right)")]
    [SerializeField] private HandUI[] hands = new HandUI[2];

    // ==== REFERENCIAS A MANAGERS ====
    [Header("Managers (opcionales, si no usas Singleton)")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private MoneyManager moneyManager;

    private void Awake()
    {
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
        if (gameManager == null && GameManager.Instance != null)
            gameManager = GameManager.Instance;

        if (moneyManager == null && MoneyManager.Instance != null)
            moneyManager = MoneyManager.Instance;

        if (gameManager != null)
            gameManager.OnGameStateChanged += HandleGameStateChanged;

        if (moneyManager != null)
        {
            moneyManager.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(moneyManager.CurrentMoney);
        }

        // Estado inicial: main menu en ambas manos
        ShowOnlyPanelAllHands(GameState.MainMenu);
        UpdateStateTextAllHands(GameState.MainMenu);
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameStateChanged -= HandleGameStateChanged;

        if (moneyManager != null)
            moneyManager.OnMoneyChanged -= HandleMoneyChanged;
    }

    // ----------------------------------------------------------------------
    // EVENTOS
    // ----------------------------------------------------------------------

    private void HandleGameStateChanged(GameState newState)
    {
        UpdateStateUI(newState);
    }

    private void HandleMoneyChanged(int newMoney)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h != null && h.moneyText != null)
                h.moneyText.text = $"Dinero: {newMoney}";
        }
    }

    // ----------------------------------------------------------------------
    // METODOS PUBLICOS
    // ----------------------------------------------------------------------

    public void ShowDayIntro(int day, int quota)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h == null) continue;

            if (h.dayText != null) h.dayText.text = $"Dia: {day}";
            if (h.quotaText != null) h.quotaText.text = $"Cuota del dia: {quota}";
            if (h.messageText != null) h.messageText.text = $"Comienza el dia {day}. Alcanza {quota} de dinero!";
        }

        // Por defecto: blackjack HUD
        ShowOnlyPanelAllHands(GameState.PlayingBlackjack);
    }

    public void ShowDayCompleted(int day)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h != null && h.messageText != null)
                h.messageText.text = $"Dia {day} completado. Bien hecho!";
        }
    }

    public void ShowDefeatScreen()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h != null && h.messageText != null)
                h.messageText.text = "No has alcanzado la cuota. Has sido despedido.";
        }

        ShowOnlyPanelAllHands(GameState.Defeat);
    }

    public void ShowRoundIntro(int currentRound, int totalRounds)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h == null) continue;

            if (h.roundText != null) h.roundText.text = $"Ronda: {currentRound}/{totalRounds}";
            if (h.messageText != null) h.messageText.text = $"Ronda {currentRound} de {totalRounds}.";
        }
    }

    public void UpdateBlackjackPhase(BlackjackPhase phase)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h == null) continue;

            if (h.phaseText != null)
                h.phaseText.text = $"Fase: {phase}";

            if (h.messageText != null)
            {
                switch (phase)
                {
                    case BlackjackPhase.Apuestas:
                        h.messageText.text = "Fase de apuestas: los jugadores ponen sus fichas.";
                        break;
                    case BlackjackPhase.Reparto:
                        h.messageText.text = "Repartiendo cartas iniciales...";
                        break;
                    case BlackjackPhase.TurnoJugadores:
                        h.messageText.text = "Turno de los jugadores: HIT / STAND.";
                        break;
                    case BlackjackPhase.RevelarSegundaCarta:
                        h.messageText.text = "Mostrando la segunda carta del dealer.";
                        break;
                    case BlackjackPhase.TurnoDealer:
                        h.messageText.text = "Turno del dealer.";
                        break;
                    case BlackjackPhase.Resultados:
                        h.messageText.text = "Mostrando resultados de la ronda.";
                        break;
                    default:
                        h.messageText.text = "";
                        break;
                }
            }
        }
    }

    public void UpdateStateUI(GameState state)
    {
        UpdateStateTextAllHands(state);
        ShowOnlyPanelAllHands(state);
    }

    // ----------------------------------------------------------------------
    // BOTONES
    // ----------------------------------------------------------------------

    public void OnClick_StartGame()
    {
        if (gameManager != null)
            gameManager.StartGame();
    }

    public void OnClick_TogglePause()
    {
        if (gameManager != null)
            gameManager.TogglePause();
    }

    public void OnClick_RestartGame()
    {
        Debug.Log("[UIManager] OnClick_RestartGame llamado (implementa recarga de escena aqui).");
    }

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
    // UTILIDADES (DOBLE UI)
    // ----------------------------------------------------------------------

    private void ShowOnlyPanelAllHands(GameState state)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h == null) continue;

            // Desactivar todo
            if (h.mainMenuPanel != null) h.mainMenuPanel.SetActive(false);
            if (h.blackjackHUDPanel != null) h.blackjackHUDPanel.SetActive(false);
            if (h.slotsHUDPanel != null) h.slotsHUDPanel.SetActive(false);
            if (h.pausePanel != null) h.pausePanel.SetActive(false);
            if (h.victoryPanel != null) h.victoryPanel.SetActive(false);
            if (h.defeatPanel != null) h.defeatPanel.SetActive(false);

            // Activar segun estado
            GameObject panelToShow = null;
            switch (state)
            {
                case GameState.MainMenu: panelToShow = h.mainMenuPanel; break;
                case GameState.PlayingBlackjack: panelToShow = h.blackjackHUDPanel; break;
                case GameState.PlayingSlots: panelToShow = h.slotsHUDPanel; break;
                case GameState.Pause: panelToShow = h.pausePanel; break;
                case GameState.Victory: panelToShow = h.victoryPanel; break;
                case GameState.Defeat: panelToShow = h.defeatPanel; break;
                default: panelToShow = h.blackjackHUDPanel; break;
            }

            if (panelToShow != null)
                panelToShow.SetActive(true);
        }
    }

    private void UpdateStateTextAllHands(GameState state)
    {
        for (int i = 0; i < hands.Length; i++)
        {
            var h = hands[i];
            if (h != null && h.stateText != null)
                h.stateText.text = $"Estado: {state}";
        }
    }
}
