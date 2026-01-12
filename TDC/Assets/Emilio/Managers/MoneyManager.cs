using UnityEngine;
using System;

/// <summary>
/// MoneyManager: �nica fuente de verdad para el dinero del jugador/casino.
/// Nadie m�s deber�a modificar el dinero directamente.
/// </summary>
public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    // ==== CONFIGURACI�N GENERAL ====

    [Header("Configuraci�n inicial")]
    [Tooltip("Dinero inicial al empezar una nueva partida.")]
    [SerializeField] private int startingMoney = 0;

    [Header("Apuestas m�nimas")]
    [Tooltip("Apuesta m�nima base del d�a 1.")]
    [SerializeField] private int baseMinBetDay1 = 100;

    [Tooltip("Curva opcional para calcular la apuesta m�nima seg�n el d�a (X = d�a, Y = min bet).")]
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
        //Debug.Log($"[MoneyManager] Dinero seteado a: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    /// <summary>
    /// A�ade dinero (puede ser negativo, pero nunca baja de 0).
    /// </summary>
    public void AddMoney(int amount)
    {
        int newValue = currentMoney + amount;
        currentMoney = Mathf.Max(0, newValue);
        //Debug.Log($"[MoneyManager] Dinero modificado en {amount}. Nuevo total: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
    }

    // ----------------------------------------------------------------------
    //                          CONSULTAS B�SICAS
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
            //Debug.Log($"[MoneyManager] No se puede gastar {amount}, dinero actual: {currentMoney}");
            return false;
        }

        currentMoney -= amount;
        //Debug.Log($"[MoneyManager] Gasto de {amount}. Nuevo total: {currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    // ----------------------------------------------------------------------
    //                          APUESTAS M�NIMAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Devuelve la apuesta m�nima para un d�a concreto.
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

        // F�rmula simple: base * d�a (ej: D�a 1: 100, D�a 2: 200, D�a 3: 300...)
        return Mathf.Max(1, baseMinBetDay1 * day);
    }

    // ----------------------------------------------------------------------
    //                          TRAGAPERRAS
    // ----------------------------------------------------------------------

    /// <summary>
    /// Coste de una tirada de tragaperras para un d�a concreto.
    /// Por dise�o: igual a la apuesta m�nima de ese d�a.
    /// </summary>
    public int GetSlotSpinCost(int day)
    {
        return GetMinBetForDay(day);
    }

    /// <summary>
    /// Intenta pagar el coste de una tirada de tragaperras del d�a indicado.
    /// Devuelve true si se ha podido pagar.
    /// </summary>
    public bool TryPaySlotSpin(int day)
    {
        int cost = GetSlotSpinCost(day);
        return TrySpend(cost);
    }

    // ----------------------------------------------------------------------
    //                          L�GICA DE BLACKJACK
    // ----------------------------------------------------------------------

    /// <summary>
    /// Llamar cuando el jugador (la casa) acepta una apuesta de un jugador IA.
    /// Normalmente, la apuesta de la IA no afecta al dinero del casino hasta el resultado,
    /// as� que este m�todo puede ser NO-OP. Lo dejo por si quieres registrar algo.
    /// </summary>
    public void OnPlayerPlacedBet(int betAmount)
    {
        // En este juego, el dinero que est� en juego es el de los jugadores IA,
        // as� que normalmente NO restas nada al casino aqu�.
        // Este m�todo est� por si quieres loguear o llevar estad�sticas.
        //Debug.Log($"[MoneyManager] Jugador IA apuesta {betAmount} (no afecta al dinero del casino de momento).");
    }

    /// <summary>
    /// Llamar cuando la casa (dealer) gana una apuesta de 'betAmount' de un jugador IA.
    /// La casa gana 'betAmount' (o lo que t� quieras, aqu� asumo igual que lo apostado).
    /// </summary>
    public void DealerWins(int betAmount)
    {
        AddMoney(betAmount);
        //Debug.Log($"[MoneyManager] La casa gana {betAmount} de un jugador. Total: {currentMoney}");
    }

    /// <summary>
    /// Llamar cuando el jugador IA gana con una mano normal (sin blackjack).
    /// La casa paga el doble de lo apostado (beneficio neto para la IA,
    /// pero a nosotros solo nos importa el dinero que sale del casino).
    /// </summary>
    public void DealerPaysWin(int betAmount)
    {
        int payout = betAmount * 2;
        TrySpend(payout); // si por dise�o la casa jam�s se queda a 0, puedes usar AddMoney(-payout)
        //Debug.Log($"[MoneyManager] La casa paga {payout} a un jugador (victoria normal). Total: {currentMoney}");
    }

    /// <summary>
    /// Llamar cuando el jugador IA gana con BLACKJACK.
    /// Paga 3x la apuesta.
    /// </summary>
    public void DealerPaysBlackjack(int betAmount)
    {
        int payout = betAmount * 3;
        TrySpend(payout);
        //Debug.Log($"[MoneyManager] La casa paga {payout} a un jugador (BLACKJACK). Total: {currentMoney}");
    }

    /// <summary>
    /// Si quieres un m�todo gen�rico de pago (normal o blackjack),
    /// puedes usar este y pasarle el multiplicador.
    /// </summary>
    public void DealerPays(int betAmount, int multiplier)
    {
        int payout = betAmount * multiplier;
        TrySpend(payout);
        //Debug.Log($"[MoneyManager] La casa paga {payout} (x{multiplier}) a un jugador. Total: {currentMoney}");
    }

    // ----------------------------------------------------------------------
    //                          DEBUG / UTILIDADES
    // ----------------------------------------------------------------------

#if UNITY_EDITOR
    [ContextMenu("Debug/A�adir 1000 de dinero")]
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
