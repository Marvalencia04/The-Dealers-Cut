using System.Collections;
using UnityEngine;

public class Giro : MonoBehaviour
{
    [Header("Reels")]
    public ReelSpinner r1;
    public ReelSpinner r2;
    public ReelSpinner r3;

    [Header("Referencias externas")]
    public SlotPrizeManager prizeManager;
    [SerializeField] private MoneyManager moneyManager;

    [Header("Día actual")]
    public int currentDay = 1;

    [Header("Timing")]
    public float delayBetweenReels = 0.15f;

    [Header("Control de tiradas")]
    [SerializeField] private int guaranteedEvery = 10;
    private int spinCounter = 0;


    public int[] reelOrder = new int[]
    {
        1,2,3, 1,0,2,3, 1,2,3, 1,2,3, 1,2,3
    };

    [Header("Calibración (shift por reel)")]
    public int shiftR1 = 0;
    public int shiftR2 = 0;
    public int shiftR3 = 0;

    [Header("Probabilidades de símbolos")]
    public int[] symbolWeights = new int[4] { 1, 3, 3, 1 };

    private void Update()
    {
        // Giro normal
        if (Input.GetKeyDown(KeyCode.Space))
            IntentarGiro();

        // Forzar triples
        if (Input.GetKeyDown(KeyCode.Alpha1)) ForceTriple(0); // Triple 7
        if (Input.GetKeyDown(KeyCode.Alpha2)) ForceTriple(1); // Triple Campana
        if (Input.GetKeyDown(KeyCode.Alpha3)) ForceTriple(2); // Triple Cereza
        if (Input.GetKeyDown(KeyCode.Alpha4)) ForceTriple(3); // Triple BAR

        // Forzar combinación personalizada (ejemplo Ctrl+5)
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Alpha5))
            ForceResult(0, 1, 2); // 7, Campana, Cereza
    }

    // ==========================================================
    // Giro normal
    // ==========================================================
    public void IntentarGiro()
    {
        if (moneyManager == null)
            return;

        if (!moneyManager.TryPaySlotSpin(currentDay))
        {
            Debug.Log("💸 No hay dinero suficiente para girar.");
            return;
        }

        spinCounter++;

        int[] result;

        if (spinCounter >= guaranteedEvery)
        {
            result = GenerateGuaranteedTriple();
            spinCounter = 0;

            Debug.Log("🎯 TIRADA GARANTIZADA: TRIPLE FIGURA");
        }
        else
        {
            result = GenerateWeightedArray();
        }

        Debug.Log($"🎰 Tirada {spinCounter}/{guaranteedEvery}");
        PlayRequested(result);
    }
    private int[] GenerateGuaranteedTriple()
    {
        int symbol = WeightedRandom(symbolWeights);
        return new int[] { symbol, symbol, symbol };
    }


    public int[] GenerateWeightedArray()
    {
        int[] arr = new int[3];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = WeightedRandom(symbolWeights);

        Debug.Log("Resultado generado: [" + string.Join(", ", arr) + "]");
        return arr;
    }

    int WeightedRandom(int[] weights)
    {
        int total = 0;
        foreach (var w in weights) total += Mathf.Max(0, w);
        if (total == 0) return 0;

        int r = Random.Range(0, total);
        for (int i = 0; i < weights.Length; i++)
        {
            if (r < weights[i]) return i;
            r -= weights[i];
        }
        return 0;
    }

    // ==========================================================
    // Forzar resultados
    // ==========================================================

    /// <summary>
    /// Fuerza un triple de un mismo símbolo (0..3)
    /// </summary>
    public void ForceTriple(int symbol)
    {
        int[] forced = new int[3] { symbol, symbol, symbol };
        Debug.Log($"🎯 Forzando triple: [{symbol},{symbol},{symbol}]");
        PlayRequested(forced);
    }

    /// <summary>
    /// Fuerza un resultado específico de 3 símbolos
    /// </summary>
    public void ForceResult(int symbol1, int symbol2, int symbol3)
    {
        int[] forced = new int[3] { symbol1, symbol2, symbol3 };
        Debug.Log($"🎯 Forzando resultado: [{symbol1},{symbol2},{symbol3}]");
        PlayRequested(forced);
    }

    // ==========================================================
    // Animación de rodillos
    // ==========================================================
    public void PlayRequested(int[] requestSymbols)
    {
        if (requestSymbols == null || requestSymbols.Length != 3)
        {
            Debug.LogError("PlayRequested necesita un array de 3 símbolos [0..3].");
            return;
        }

        StartCoroutine(SpinAllSequential(requestSymbols));
    }

    IEnumerator SpinAllSequential(int[] req)
    {
        int idx1 = FindNextIndexForSymbol(r1, req[0], shiftR1);
        r1.SpinToIndex(idx1);
        yield return new WaitForSeconds(delayBetweenReels);

        int idx2 = FindNextIndexForSymbol(r2, req[1], shiftR2);
        r2.SpinToIndex(idx2);
        yield return new WaitForSeconds(delayBetweenReels);

        int idx3 = FindNextIndexForSymbol(r3, req[2], shiftR3);
        r3.SpinToIndex(idx3);

        float timeout = Mathf.Max(r1.spinDuration, r2.spinDuration, r3.spinDuration) + 2f;
        float elapsed = 0f;

        /*while ((r1.IsSpinning || r2.IsSpinning || r3.IsSpinning) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }*/

        if (elapsed >= timeout)
            Debug.LogWarning("Timeout esperando rodillos.");

        if (prizeManager != null)
        {
            Debug.Log("Evaluando resultado final: [" + string.Join(", ", req) + "]");
            prizeManager.EvaluarResultado(req);
        }
        else
        {
            Debug.LogWarning("No hay PrizeManager asignado.");
        }
    }

    // ==========================================================
    // Cálculo del siguiente índice para el rodillo
    // ==========================================================
    int FindNextIndexForSymbol(ReelSpinner reel, int symbol, int shift)
    {
        int items = Mathf.Max(1, reel.items);
        int from = reel.GetApproxIndex();
        shift = ((shift % items) + items) % items;

        for (int step = 1; step <= items; step++)
        {
            int i = (from + step) % items;
            if (reelOrder[(i + shift) % items] == symbol)
                return i;
        }

        for (int i = 0; i < items; i++)
            if (reelOrder[(i + shift) % items] == symbol)
                return i;

        return from;
    }
}
/*using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

[Serializable]
public class TrapSpriteData
{
    public TrapType trapType;
    public Sprite sprite;
}

/// <summary>
/// Tipos de trampas disponibles.
/// </summary>
public enum TrapType
{
    MazoVisible,
    CartaAElegir,
    LlamadaSeguridad,
    NormaDealer,
    MiraAlli
}

/// <summary>
/// Rareza de las trampas (para recompensas de la tragaperras).
/// </summary>
public enum TrapRarity
{
    Comun,
    Rara,
    Epica,
    Legendaria
}

/// <summary>
/// TrampasManager: gestiona usos, disponibilidad por fase
/// y activación de trampas, además de actualizar la UI.
/// </summary>
public class TrampasManager : MonoBehaviour
{
    public static TrampasManager Instance { get; private set; }

    // ==== CONFIGURACIÓN INICIAL ====
    [Header("Sprites de trampas")]
    [SerializeField] private TrapSpriteData[] trapSprites;


    [Header("Usos iniciales por trampa")]
    [SerializeField] private int usosInicialMazoVisible = 3;
    [SerializeField] private int usosInicialCartaAElegir = 1;
    [SerializeField] private int usosInicialLlamadaSeguridad = 1;
    [SerializeField] private int usosInicialNormaDealer = 3;
    [SerializeField] private int usosInicialMiraAlli = 1;

    // ==== ESTADO ACTUAL ====

    private int usosMazoVisible;
    private int usosCartaAElegir;
    private int usosLlamadaSeguridad;
    private int usosNormaDealer;
    private int usosMiraAlli;

    private BlackjackPhase currentPhase = BlackjackPhase.None;

    // ==== REFERENCIAS ====

    [Header("Referencias")]
    [SerializeField] private BlackjackTable blackjackTable;   // Implementas tú este script
    [SerializeField] private UIManager uiManager;             // Opcional, por si quieres mensajes extra

    // ==== UI DE TRAMPAS ====

    [Header("UI - Botones de trampas")]
    [SerializeField] private Button btnMazoVisible;
    [SerializeField] private Button btnCartaAElegir;
    [SerializeField] private Button btnLlamadaSeguridad;
    [SerializeField] private Button btnNormaDealer;
    [SerializeField] private Button btnMiraAlli;

    [Header("UI - Textos de usos restantes")]
    [SerializeField] private TextMeshProUGUI txtMazoVisibleUsos;
    [SerializeField] private TextMeshProUGUI txtCartaAElegirUsos;
    [SerializeField] private TextMeshProUGUI txtLlamadaSeguridadUsos;
    [SerializeField] private TextMeshProUGUI txtNormaDealerUsos;
    [SerializeField] private TextMeshProUGUI txtMiraAlliUsos;

    // Mensajes generales (opcional)
    [Header("UI - Mensajes")]
    [SerializeField] private Text txtMensajeTrampas;

    // Evento opcional para cuando cambian usos (otras UI pueden escuchar)
    public event Action OnTrapsChanged;

    // ----------------------------------------------------------------------
    // CICLO DE VIDA
    // ----------------------------------------------------------------------
    public Sprite GetTrapSprite(TrapType type)
    {
        foreach (var data in trapSprites)
        {
            if (data.trapType == type)
                return data.sprite;
        }

        Debug.LogWarning($"[TrampasManager] No hay sprite asignado para {type}");
        return null;
    }

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
        ResetUsosToInitial();
        UpdateTrapsUI();
    }

    // ----------------------------------------------------------------------
    // INICIALIZACIÓN / RESET
    // ----------------------------------------------------------------------

    /// <summary>
    /// Restaura los usos a los valores iniciales configurados en el inspector.
    /// </summary>
    public void ResetUsosToInitial()
    {
        usosMazoVisible = usosInicialMazoVisible;
        usosCartaAElegir = usosInicialCartaAElegir;
        usosLlamadaSeguridad = usosInicialLlamadaSeguridad;
        usosNormaDealer = usosInicialNormaDealer;
        usosMiraAlli = usosInicialMiraAlli;

        OnTrapsChanged?.Invoke();
    }

    // ----------------------------------------------------------------------
    // CAMBIO DE FASE (LLAMADO DESDE RondaManager)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamado desde RondaManager cuando cambia la fase del Blackjack.
    /// Aquí decidimos qué trampas se pueden usar.
    /// </summary>
    public void OnBlackjackPhaseChanged(BlackjackPhase phase)
    {
        currentPhase = phase;
        UpdateTrapsUI();
    }

    // ----------------------------------------------------------------------
    // UI: ACTUALIZAR BOTONES Y TEXTOS
    // ----------------------------------------------------------------------

    private void UpdateTrapsUI()
    {
        // Textos de usos
        if (txtMazoVisibleUsos != null) txtMazoVisibleUsos.text = usosMazoVisible.ToString();
        if (txtCartaAElegirUsos != null) txtCartaAElegirUsos.text = usosCartaAElegir.ToString();
        if (txtLlamadaSeguridadUsos != null) txtLlamadaSeguridadUsos.text = usosLlamadaSeguridad.ToString();
        if (txtNormaDealerUsos != null) txtNormaDealerUsos.text = usosNormaDealer.ToString();
        if (txtMiraAlliUsos != null) txtMiraAlliUsos.text = usosMiraAlli.ToString();

        // Botones: interactables según usos y fase
        if (btnMazoVisible != null)
            btnMazoVisible.interactable = usosMazoVisible > 0 && IsTrapAllowedInCurrentPhase(TrapType.MazoVisible);

        if (btnCartaAElegir != null)
            btnCartaAElegir.interactable = usosCartaAElegir > 0 && IsTrapAllowedInCurrentPhase(TrapType.CartaAElegir);

        if (btnLlamadaSeguridad != null)
            btnLlamadaSeguridad.interactable = usosLlamadaSeguridad > 0 && IsTrapAllowedInCurrentPhase(TrapType.LlamadaSeguridad);

        if (btnNormaDealer != null)
            btnNormaDealer.interactable = usosNormaDealer > 0 && IsTrapAllowedInCurrentPhase(TrapType.NormaDealer);

        if (btnMiraAlli != null)
            btnMiraAlli.interactable = usosMiraAlli > 0 && IsTrapAllowedInCurrentPhase(TrapType.MiraAlli);
    }

    // ----------------------------------------------------------------------
    // LÓGICA DE FASES PERMITIDAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Devuelve true si una trampa se puede usar en la fase actual,
    /// según el documento de diseño.
    /// </summary>
    private bool IsTrapAllowedInCurrentPhase(TrapType type)
    {
        switch (type)
        {
            case TrapType.MazoVisible:
                // Mazo visible: fases 1,2,3,5 (Apuestas, Reparto, TurnoJugadores, TurnoDealer)
                return currentPhase == BlackjackPhase.Apuestas ||
                       currentPhase == BlackjackPhase.Reparto ||
                       currentPhase == BlackjackPhase.TurnoJugadores ||
                       currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.CartaAElegir:
                // Carta a elegir: fases 1,2,3,5
                return currentPhase == BlackjackPhase.Apuestas ||
                       currentPhase == BlackjackPhase.Reparto ||
                       currentPhase == BlackjackPhase.TurnoJugadores ||
                       currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.LlamadaSeguridad:
                // Llamada de seguridad: todas las fases
                return true;

            case TrapType.NormaDealer:
                // Norma del dealer: solo fase 5 (TurnoDealer)
                return currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.MiraAlli:
                // Mira allí: todas las fases
                return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    // USO DE TRAMPAS (LLAMADO POR BOTONES UI)
    // ----------------------------------------------------------------------

    /// <summary>
    /// Intenta usar una trampa. Valida usos y fase.
    /// </summary>
    private bool TryUseTrap(TrapType type)
    {
        if (!IsTrapAllowedInCurrentPhase(type))
        {
            ShowTrapMessage("No puedes usar esta trampa en esta fase.");
            return false;
        }

        switch (type)
        {
            case TrapType.MazoVisible:
                if (usosMazoVisible <= 0) { ShowTrapMessage("No te quedan usos de Mazo visible."); return false; }
                usosMazoVisible--;
                break;

            case TrapType.CartaAElegir:
                if (usosCartaAElegir <= 0) { ShowTrapMessage("No te quedan usos de Carta a elegir."); return false; }
                usosCartaAElegir--;
                break;

            case TrapType.LlamadaSeguridad:
                if (usosLlamadaSeguridad <= 0) { ShowTrapMessage("No te quedan usos de Llamada de seguridad."); return false; }
                usosLlamadaSeguridad--;
                break;

            case TrapType.NormaDealer:
                if (usosNormaDealer <= 0) { ShowTrapMessage("No te quedan usos de Norma del dealer."); return false; }
                usosNormaDealer--;
                break;

            case TrapType.MiraAlli:
                if (usosMiraAlli <= 0) { ShowTrapMessage("No te quedan usos de Mira allí."); return false; }
                usosMiraAlli--;
                break;
        }

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();
        return true;
    }

    // --- Botones UI ---

    public void OnClick_MazoVisible()
    {
        if (!TryUseTrap(TrapType.MazoVisible)) return;

        if (blackjackTable != null)
        {
            // Dentro de BlackjackTable tú implementas la lógica real
            blackjackTable.ShowNextThreeCards();
        }

        ShowTrapMessage("Has usado Mazo visible: ves las siguientes 3 cartas.");
    }

    public void OnClick_CartaAElegir()
    {
        if (!TryUseTrap(TrapType.CartaAElegir)) return;

        if (blackjackTable != null)
        {
            // Mostrar UI de 3 cartas y dejar que el jugador elija una
            blackjackTable.StartCartaAElegirMode();
        }

        ShowTrapMessage("Has usado Carta a elegir: elige una de 3 cartas.");
    }

    public void OnClick_LlamadaSeguridad()
    {
        if (!TryUseTrap(TrapType.LlamadaSeguridad)) return;

        if (blackjackTable != null)
        {
            // Activa modo de seleccionar jugador para echarlo de la mesa
            blackjackTable.StartLlamadaSeguridadMode();
        }

        ShowTrapMessage("Has llamado a seguridad: elige un jugador para expulsar.");
    }

    public void OnClick_NormaDealer()
    {
        if (!TryUseTrap(TrapType.NormaDealer)) return;

        if (blackjackTable != null)
        {
            // El dealer puede ignorar las reglas de robar/pararse esta ronda
            blackjackTable.EnableIgnoreDealerRulesForThisRound();
        }

        ShowTrapMessage("Has alterado la norma del dealer para esta ronda.");
    }

    public void OnClick_MiraAlli()
    {
        if (!TryUseTrap(TrapType.MiraAlli)) return;

        if (blackjackTable != null)
        {
            // Activa modo “Mira allí”: los jugadores se distraen durante X segundos
            // Dentro de BlackjackTable, controla el tiempo y penalización si hay cartas mal.
            blackjackTable.StartMiraAlliMode();
        }

        ShowTrapMessage("Has usado Mira allí: tienes unos segundos para manipular.");
    }

    // ----------------------------------------------------------------------
    // RECOMPENSAS DESDE TRAGAPERRAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamado desde SlotsManager cuando se obtiene una recompensa
    /// de determinada rareza. Devuelve usos de trampas.
    /// </summary>
    public void RecoverTrap(TrapRarity rarity)
    {
        switch (rarity)
        {
            case TrapRarity.Comun:
                // 50/50 entre MazoVisible y NormaDealer
                if (UnityEngine.Random.value < 0.5f)
                {
                    usosMazoVisible++;
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Mazo Visible (Común).");
                }
                else
                {
                    usosNormaDealer++;
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Norma Dealer (Común).");
                }
                break;

            case TrapRarity.Rara:
                usosCartaAElegir++;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Carta a elegir (Rara).");
                break;

            case TrapRarity.Epica:
                usosMiraAlli++;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Mira allí (Épica).");
                break;

            case TrapRarity.Legendaria:
                usosLlamadaSeguridad++;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Llamada de seguridad (Legendaria).");
                break;
        }

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();
    }

    // ----------------------------------------------------------------------
    // MENSAJES
    // ----------------------------------------------------------------------

    private void ShowTrapMessage(string msg)
    {
        if (txtMensajeTrampas != null)
            txtMensajeTrampas.text = msg;

        // También puedes mandar el mensaje a UIManager si quieres algo global:
        if (uiManager != null)
        {
            // Por ejemplo, reutilizando messageText general si lo tienes público.
            // uiManager.ShowCustomMessage(msg);
        }

        Debug.Log("[TrampasManager] " + msg);
    }
    public void LogUsosActuales()
    {
        Debug.Log($"[DEBUG] Usos actuales de trampas:");
        Debug.Log($"MazoVisible: {usosMazoVisible}");
        Debug.Log($"CartaAElegir: {usosCartaAElegir}");
        Debug.Log($"LlamadaSeguridad: {usosLlamadaSeguridad}");
        Debug.Log($"NormaDealer: {usosNormaDealer}");
        Debug.Log($"MiraAlli: {usosMiraAlli}");
    }

}
*/