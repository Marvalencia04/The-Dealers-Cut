using UnityEngine;
using System;

/// <summary>
/// MoneyManager: única fuente de verdad para el dinero del jugador/casino.
/// Nadie más debería modificar el dinero directamente.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    // ==== CONFIGURACIÓN GENERAL ====

    [Header("Configuración inicial")]
    [Tooltip("Dinero inicial al empezar una nueva partida.")]
    [SerializeField] private int startingMoney = 0;

    [Header("Apuestas mínimas")]
    [Tooltip("Apuesta mínima base del día 1.")]
    [SerializeField] private int baseMinBetDay1 = 100;

    [Tooltip("Curva opcional para calcular la apuesta mínima según el día (X = día, Y = min bet).")]
    [SerializeField] private AnimationCurve minBetByDayCurve;

    // ==== ESTADO ACTUAL ====

    [SerializeField] private int currentMoney = 0;
    public int CurrentMoney => currentMoney;

    /// <summary>
    /// Evento llamado cuando cambia el dinero.
    /// La UI u otros sistemas pueden suscribirse para actualizarse.
    /// </summary>
    public event Action<int> OnMoneyChanged;

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
        // Si quieres que arranque con el dinero inicial siempre:
        SetMoney(startingMoney);
    }

    // ----------------------------------------------------------------------
    //                          SET/GET DINERO
    // ----------------------------------------------------------------------

    /// <summary>
    /// Establece el dinero actual (se fuerza a >= 0).
    /// </summary>
    public void SetMoney(int amount)
    {
        currentMoney = Mathf.Max(0, amount);
        Debug.Log($"[MoneyManager] Dinero seteado a: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>
    /// Añade dinero (puede ser negativo, pero nunca baja de 0).
    /// </summary>
    public void AddMoney(int amount)
    {
        int newValue = currentMoney + amount;
        currentMoney = Mathf.Max(0, newValue);
        Debug.Log($"[MoneyManager] Dinero modificado en {amount}. Nuevo total: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // ----------------------------------------------------------------------
    //                          CONSULTAS BÁSICAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// True si el jugador puede permitirse gastar 'amount'.
    /// </summary>
    public bool CanAfford(int amount)
    {
        return currentMoney >= amount;
    }

    /// <summary>
    /// Intenta gastar 'amount'. Devuelve true si ha sido posible.
    /// </summary>
    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
        {
            Debug.Log($"[MoneyManager] No se puede gastar {amount}, dinero actual: {currentMoney}");
            return false;
        }

        currentMoney -= amount;
        Debug.Log($"[MoneyManager] Gasto de {amount}. Nuevo total: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    // ----------------------------------------------------------------------
    //                          APUESTAS MÍNIMAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Devuelve la apuesta mínima para un día concreto.
    /// Se usa para Blackjack y para el coste de tirada de tragaperras.
    /// </summary>
    public int GetMinBetForDay(int day)
    {
        if (day <= 0) day = 1;

        if (minBetByDayCurve != null && minBetByDayCurve.length > 0)
        {
            float value = minBetByDayCurve.Evaluate(day);
            int betFromCurve = Mathf.Max(1, Mathf.RoundToInt(value));
            if (betFromCurve > 0)
                return betFromCurve;
        }

        // Fórmula simple: base * día (ej: Día 1: 100, Día 2: 200, Día 3: 300...)
        return Mathf.Max(1, baseMinBetDay1 * day);
    }

    // ----------------------------------------------------------------------
    //                          TRAGAPERRAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Coste de una tirada de tragaperras para un día concreto.
    /// Por diseño: igual a la apuesta mínima de ese día.
    /// </summary>
    public int GetSlotSpinCost(int day)
    {
        return GetMinBetForDay(day);
    }

    /// <summary>
    /// Intenta pagar el coste de una tirada de tragaperras del día indicado.
    /// Devuelve true si se ha podido pagar.
    /// </summary>
    public bool TryPaySlotSpin(int day)
    {
        int cost = GetSlotSpinCost(day);
        return TrySpend(cost);
    }

    // ----------------------------------------------------------------------
    //                          LÓGICA DE BLACKJACK
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamar cuando el jugador (la casa) acepta una apuesta de un jugador IA.
    /// Normalmente, la apuesta de la IA no afecta al dinero del casino hasta el resultado,
    /// así que este método puede ser NO-OP. Lo dejo por si quieres registrar algo.
    /// </summary>
    public void OnPlayerPlacedBet(int betAmount)
    {
        // En este juego, el dinero que está en juego es el de los jugadores IA,
        // así que normalmente NO restas nada al casino aquí.
        // Este método está por si quieres loguear o llevar estadísticas.
        Debug.Log($"[MoneyManager] Jugador IA apuesta {betAmount} (no afecta al dinero del casino de momento).");
    }

    /// <summary>
    /// Llamar cuando la casa (dealer) gana una apuesta de 'betAmount' de un jugador IA.
    /// La casa gana 'betAmount' (o lo que tú quieras, aquí asumo igual que lo apostado).
    /// </summary>
    public void DealerWins(int betAmount)
    {
        AddMoney(betAmount);
        Debug.Log($"[MoneyManager] La casa gana {betAmount} de un jugador. Total: {currentMoney}");
    }

    /// <summary>
    /// Llamar cuando el jugador IA gana con una mano normal (sin blackjack).
    /// La casa paga el doble de lo apostado (beneficio neto para la IA,
    /// pero a nosotros solo nos importa el dinero que sale del casino).
    /// </summary>
    public void DealerPaysWin(int betAmount)
    {
        int payout = betAmount * 2;
        TrySpend(payout); // si por diseño la casa jamás se queda a 0, puedes usar AddMoney(-payout)
        Debug.Log($"[MoneyManager] La casa paga {payout} a un jugador (victoria normal). Total: {currentMoney}");
    }

    /// <summary>
    /// Llamar cuando el jugador IA gana con BLACKJACK.
    /// Paga 3x la apuesta.
    /// </summary>
    public void DealerPaysBlackjack(int betAmount)
    {
        int payout = betAmount * 3;
        TrySpend(payout);
        Debug.Log($"[MoneyManager] La casa paga {payout} a un jugador (BLACKJACK). Total: {currentMoney}");
    }

    /// <summary>
    /// Si quieres un método genérico de pago (normal o blackjack),
    /// puedes usar este y pasarle el multiplicador.
    /// </summary>
    public void DealerPays(int betAmount, int multiplier)
    {
        int payout = betAmount * multiplier;
        TrySpend(payout);
        Debug.Log($"[MoneyManager] La casa paga {payout} (x{multiplier}) a un jugador. Total: {currentMoney}");
    }

    // ----------------------------------------------------------------------
    //                          DEBUG / UTILIDADES
    // ----------------------------------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("Debug/Añadir 1000 de dinero")]
    private void Debug_AddMoney()
    {
        AddMoney(1000);
    }

    [ContextMenu("Debug/Quitar 500 de dinero")]
    private void Debug_RemoveMoney()
    {
        TrySpend(500);
    }
#endif
}
