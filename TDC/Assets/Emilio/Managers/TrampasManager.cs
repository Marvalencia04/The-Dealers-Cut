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
/// y activacion de trampas, ademas de actualizar la UI.
/// 
/// Version: Doble UI (Canvas en cada mano)
/// - Dos grupos de botones/textos (Left/Right) en el Inspector
/// - Misma logica, se refleja en ambas manos
/// - Autobindeo opcional de botones (evita configurar OnClick en cada canvas)
/// </summary>
public class TrampasManager : MonoBehaviour
{
    public static TrampasManager Instance { get; private set; }

    // ==== CONFIGURACION INICIAL ====

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
    [SerializeField] private BlackjackTable blackjackTable;
    [SerializeField] private UIManager uiManager;

    // ==== TRAP SCRIPTS (solo funcionalidad) ====

    [Header("Trap Scripts (solo funcionalidad)")]
    [SerializeField] private DealerNormaTrap normaDealerTrap;

    // ==== DOBLE UI ====

    [Serializable]
    private class TrapsHandUI
    {
        [Header("UI - Botones de trampas")]
        public Button btnMazoVisible;
        public Button btnCartaAElegir;
        public Button btnLlamadaSeguridad;
        public Button btnNormaDealer;
        public Button btnMiraAlli;

        [Header("UI - Textos de usos restantes")]
        public TextMeshProUGUI txtMazoVisibleUsos;
        public TextMeshProUGUI txtCartaAElegirUsos;
        public TextMeshProUGUI txtLlamadaSeguridadUsos;
        public TextMeshProUGUI txtNormaDealerUsos;
        public TextMeshProUGUI txtMiraAlliUsos;

        [Header("UI - Mensajes (opcional por mano)")]
        public TextMeshProUGUI txtMensajeTrampas;
    }

    [Header("UI - Manos (0 = Izquierda, 1 = Derecha)")]
    [SerializeField] private TrapsHandUI[] handUI = new TrapsHandUI[2];

    [Header("UI - Opciones")]
    [Tooltip("Si esta activo, este script vincula automaticamente los onClick de los botones de ambas manos.")]
    [SerializeField] private bool autoBindButtonsOnAwake = true;

    [Header("Trap Scripts (solo funcionalidad)")]
    [SerializeField] private LlamadaSeguridadTrap llamadaSeguridadTrap;


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

        if (autoBindButtonsOnAwake)
            AutoBindButtons();
    }

    private void Start()
    {
        ResetUsosToInitial();
        UpdateTrapsUI();
    }

    // ----------------------------------------------------------------------
    // INICIALIZACION / RESET
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
    /// Aqui decidimos que trampas se pueden usar.
    /// </summary>
    public void OnBlackjackPhaseChanged(BlackjackPhase phase)
    {
        currentPhase = phase;
        UpdateTrapsUI();
    }

    // ----------------------------------------------------------------------
    // UI: ACTUALIZAR BOTONES Y TEXTOS (DOBLE UI)
    // ----------------------------------------------------------------------

    private void UpdateTrapsUI()
    {
        for (int i = 0; i < handUI.Length; i++)
        {
            var h = handUI[i];
            if (h == null) continue;

            // Textos de usos
            if (h.txtMazoVisibleUsos != null) h.txtMazoVisibleUsos.text = usosMazoVisible.ToString();
            if (h.txtCartaAElegirUsos != null) h.txtCartaAElegirUsos.text = usosCartaAElegir.ToString();
            if (h.txtLlamadaSeguridadUsos != null) h.txtLlamadaSeguridadUsos.text = usosLlamadaSeguridad.ToString();
            if (h.txtNormaDealerUsos != null) h.txtNormaDealerUsos.text = usosNormaDealer.ToString();
            if (h.txtMiraAlliUsos != null) h.txtMiraAlliUsos.text = usosMiraAlli.ToString();

            // Botones: interactables segun usos y fase
            if (h.btnMazoVisible != null)
                h.btnMazoVisible.interactable = usosMazoVisible > 0 && IsTrapAllowedInCurrentPhase(TrapType.MazoVisible);

            if (h.btnCartaAElegir != null)
                h.btnCartaAElegir.interactable = usosCartaAElegir > 0 && IsTrapAllowedInCurrentPhase(TrapType.CartaAElegir);

            if (h.btnLlamadaSeguridad != null)
                if (h.btnLlamadaSeguridad != null)
                    h.btnLlamadaSeguridad.interactable =
                        usosLlamadaSeguridad > 0 &&
                        IsTrapAllowedInCurrentPhase(TrapType.LlamadaSeguridad) &&
                        CanUseLlamadaSeguridadNow();

            if (h.btnNormaDealer != null)
                h.btnNormaDealer.interactable = usosNormaDealer > 0 && IsTrapAllowedInCurrentPhase(TrapType.NormaDealer);

            if (h.btnMiraAlli != null)
                h.btnMiraAlli.interactable = usosMiraAlli > 0 && IsTrapAllowedInCurrentPhase(TrapType.MiraAlli);
        }
    }

    // ----------------------------------------------------------------------
    // L�GICA DE FASES PERMITIDAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Devuelve true si una trampa se puede usar en la fase actual.
    /// </summary>
    private bool IsTrapAllowedInCurrentPhase(TrapType type)
    {
        switch (type)
        {
            case TrapType.MazoVisible:
                // Fases 1,2,3,5
                return currentPhase == BlackjackPhase.Apuestas ||
                       currentPhase == BlackjackPhase.Reparto ||
                       currentPhase == BlackjackPhase.TurnoJugadores ||
                       currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.CartaAElegir:
                // Fases 1,2,3,5
                return currentPhase == BlackjackPhase.Apuestas ||
                       currentPhase == BlackjackPhase.Reparto ||
                       currentPhase == BlackjackPhase.TurnoJugadores ||
                       currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.LlamadaSeguridad:
                // Todas las fases
                return true;

            case TrapType.NormaDealer:
                // Solo TurnoDealer
                return currentPhase == BlackjackPhase.TurnoDealer;

            case TrapType.MiraAlli:
                // Todas las fases
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

        //Debug.LogWarning($"[TrampasManager] No hay sprite asignado para {type}");
        return null;
    }
    // ----------------------------------------------------------------------
    // USO DE TRAMPAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Intenta usar una trampa. Valida usos y fase.
    /// (Para NormaDealer NO se usa, porque esa trampa puede fallar y no queremos consumir uso).
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
                if (usosMiraAlli <= 0) { ShowTrapMessage("No te quedan usos de Mira alli."); return false; }
                usosMiraAlli--;
                break;
        }

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();
        return true;
    }

    // ----------------------------------------------------------------------
    // BOTONES (se pueden llamar desde cualquier canvas)
    // ----------------------------------------------------------------------

    public void OnClick_MazoVisible()
    {
        if (!TryUseTrap(TrapType.MazoVisible)) return;

        if (blackjackTable != null)
            blackjackTable.ShowNextThreeCards();

        ShowTrapMessage("Has usado Mazo visible: ves las siguientes 3 cartas.");
    }

    public void OnClick_CartaAElegir()
    {
        if (!TryUseTrap(TrapType.CartaAElegir)) return;

        if (blackjackTable != null)
            blackjackTable.StartCartaAElegirMode();

        ShowTrapMessage("Has usado Carta a elegir: elige una de 3 cartas.");
    }

    public void OnClick_LlamadaSeguridad()
    {
        if (blackjackTable == null)
        {
            ShowTrapMessage("BlackjackTable no asignada en TrampasManager.");
            return;
        }

        if (!CanUseLlamadaSeguridadNow())
        {
            ShowTrapMessage("Aún no: espera a que todos tengan sus 2 cartas iniciales.");
            return;
        }


        // Validar fase/usos, pero NO consumimos aun
        if (!IsTrapAllowedInCurrentPhase(TrapType.LlamadaSeguridad))
        {
            ShowTrapMessage("No puedes usar esta trampa en esta fase.");
            return;
        }

        if (usosLlamadaSeguridad <= 0)
        {
            ShowTrapMessage("No te quedan usos de Llamada de seguridad.");
            return;
        }

        if (llamadaSeguridadTrap == null)
        {
            ShowTrapMessage("LlamadaSeguridadTrap no asignada.");
            return;
        }

        llamadaSeguridadTrap.BeginSelection();
        ShowTrapMessage("Modo seguridad: selecciona un NPC.");
    }


    public void OnClick_NormaDealer()
    {
        //Debug.Log("NorMa Dealer Activada");
        // NormaDealer: valida fase/usos, ejecuta, si no se aplica NO consume.
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

        bool applied = false;

        if (normaDealerTrap != null)
            applied = normaDealerTrap.Apply();
        else if (blackjackTable != null)
            applied = blackjackTable.ApplyNormaDealer_ForceStandNow();

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
            blackjackTable.StartMiraAlliMode();

        ShowTrapMessage("Has usado Mira alli: tienes unos segundos para manipular.");
    }


    public void TrySelectSecurityTarget(SecurityTarget target)
    {
        if (llamadaSeguridadTrap == null) return;
        if (!llamadaSeguridadTrap.IsSelecting) return;

        bool applied = llamadaSeguridadTrap.TryApply(target);
        if (!applied) return;

        // Si se aplico, consumimos uso
        usosLlamadaSeguridad--;

        OnTrapsChanged?.Invoke();
        UpdateTrapsUI();

        ShowTrapMessage("Seguridad se lo llevo. Apuesta robada.");
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
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Mazo Visible (Com�n).");
                }
                else
                {
                    usosNormaDealer++;
                    rewardedTrap = TrapType.NormaDealer;
                    ShowTrapMessage("La tragaperras te ha dado +1 uso de Norma Dealer (Com�n).");
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
                ShowTrapMessage("La tragaperras te ha dado +1 uso de Mira all� (�pica).");
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
    // MENSAJES (DOBLE UI)
    // ----------------------------------------------------------------------

    private void ShowTrapMessage(string msg)
    {
        // Mensaje en ambas manos (si existe)
        for (int i = 0; i < handUI.Length; i++)
        {
            var h = handUI[i];
            if (h != null && h.txtMensajeTrampas != null)
                h.txtMensajeTrampas.text = msg;
        }

        // Opcional: mensaje global
        if (uiManager != null)
        {
            // Si tu UIManager tiene un metodo para mensajes globales, llamalo aqui.
            // uiManager.ShowMessage(msg);
        }

        //Debug.Log("[TrampasManager] " + msg);
    }

    public void LogUsosActuales()
    {
        Debug.Log("[DEBUG] Usos actuales de trampas:");
        Debug.Log("MazoVisible: " + usosMazoVisible);
        Debug.Log("CartaAElegir: " + usosCartaAElegir);
        Debug.Log("LlamadaSeguridad: " + usosLlamadaSeguridad);
        Debug.Log("NormaDealer: " + usosNormaDealer);
        Debug.Log("MiraAlli: " + usosMiraAlli);
    }

    // ----------------------------------------------------------------------
    // AUTOBIND DE BOTONES (DOBLE UI)
    // ----------------------------------------------------------------------

    private void AutoBindButtons()
    {
        for (int i = 0; i < handUI.Length; i++)
        {
            var h = handUI[i];
            if (h == null) continue;

            Bind(h.btnMazoVisible, OnClick_MazoVisible);
            Bind(h.btnCartaAElegir, OnClick_CartaAElegir);
            Bind(h.btnLlamadaSeguridad, OnClick_LlamadaSeguridad);
            Bind(h.btnNormaDealer, OnClick_NormaDealer);
            Bind(h.btnMiraAlli, OnClick_MiraAlli);
        }
    }

    private void Bind(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null) return;
        b.onClick.RemoveListener(action);
        b.onClick.AddListener(action);
    }

    private bool CanUseLlamadaSeguridadNow()
    {
        if (blackjackTable == null) return false;
        return blackjackTable.AreInitialHandsComplete(2);
    }


}
