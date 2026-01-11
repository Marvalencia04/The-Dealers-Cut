using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

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
[Serializable]
public class TrapSpriteData
{
    public TrapType trapType;
    public Sprite sprite;
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

    // ==== SPRITES DE TRAMPAS ====
    [Header("Sprites de trampas")]
    [SerializeField] private TrapSpriteData[] trapSprites;


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

    [Header("Trap Scripts (solo funcionalidad)")]
    [SerializeField] private DealerNormaTrap normaDealerTrap;


    // Evento opcional para cuando cambian usos (otras UI pueden escuchar)
    public event Action OnTrapsChanged;

    // ----------------------------------------------------------------------
    // CICLO DE VIDA
    // ----------------------------------------------------------------------

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
        // 1) Validar fase y usos, PERO NO consumimos aun
        if (!IsTrapAllowedInCurrentPhase(TrapType.NormaDealer))
        {
            ShowTrapMessage("No puedes usar esta trampa en esta fase.");
            return;
        }

        if (usosNormaDealer <= 0)
        {
            ShowTrapMessage("No te quedan usos de Norma del dealer.");
            return;
        }

        // 2) Ejecutar efecto
        bool applied = false;

        // Si has añadido la referencia al script (recomendado)
        if (normaDealerTrap != null)
            applied = normaDealerTrap.Apply();
        else if (blackjackTable != null)
            applied = blackjackTable.ApplyNormaDealer_ForceStandNow();

        // 3) Si se aplico, consumimos uso. Si no, no consumimos.
        if (!applied)
        {
            ShowTrapMessage("No se puede usar: el dealer ya tiene 17 o mas.");
            return;
        }

        usosNormaDealer--;

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();

        ShowTrapMessage("Has usado Norma del dealer: el dealer se planta aunque tenga menos de 17.");
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
    public TrapType RecoverTrap(TrapRarity rarity)
    {
        TrapType rewardedTrap = TrapType.MazoVisible;

        switch (rarity)
        {
            case TrapRarity.Comun:
                if (UnityEngine.Random.value < 0.5f)
                {
                    usosMazoVisible++;
                    rewardedTrap = TrapType.MazoVisible;
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Mazo Visible (Común).");
                }
                else
                {
                    usosNormaDealer++;
                    rewardedTrap = TrapType.NormaDealer;
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Norma Dealer (Común).");
                }
                break;

            case TrapRarity.Rara:
                usosCartaAElegir++;
                rewardedTrap = TrapType.CartaAElegir;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Carta a elegir (Rara).");
                break;

            case TrapRarity.Epica:
                usosMiraAlli++;
                rewardedTrap = TrapType.MiraAlli;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Mira allí (Épica).");
                break;

            case TrapRarity.Legendaria:
                usosLlamadaSeguridad++;
                rewardedTrap = TrapType.LlamadaSeguridad;
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Llamada de seguridad (Legendaria).");
                break;
        }

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();

        return rewardedTrap;
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
