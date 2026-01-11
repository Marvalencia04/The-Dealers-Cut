using UnityEngine;
using TMPro;

/// <summary>
/// Controla las monedas del jugador, su gasto, la UI y las pantallas de fin de juego.
/// </summary>
public class SlotCurrencyManager : MonoBehaviour
{
    [Header("Configuración de monedas")]
    [Tooltip("Cantidad inicial de monedas del jugador.")]
    public int monedasIniciales = 50;

    [Tooltip("Cantidad máxima de monedas que puede tener el jugador.")]
    public int monedasMaximas = 100;

    [Tooltip("Costo en monedas por cada tirada (Play).")]
    public int costePorJugada = 5;

    [Header("Referencias UI")]
    [Tooltip("Texto que muestra las monedas actuales y máximas (formato XX/YY).")]
    public TextMeshProUGUI monedasTexto;

    [Header("Pantallas de estado")]
    [Tooltip("Pantalla que se muestra cuando el jugador se queda sin monedas.")]
    public GameObject pantallaDerrota;

    [Tooltip("Pantalla que se muestra cuando el jugador alcanza el objetivo.")]
    public GameObject pantallaVictoria;

    private int monedasActuales;
    private bool juegoTerminado = false;

    private void Start()
    {
        monedasActuales = monedasIniciales;
        ActualizarUI();

        // Asegurarse de que las pantallas estén ocultas al iniciar
        if (pantallaDerrota) pantallaDerrota.SetActive(false);
        if (pantallaVictoria) pantallaVictoria.SetActive(false);
    }

    /// <summary>
    /// Intenta restar el costo por jugar. Devuelve true si hay saldo suficiente.
    /// </summary>
    public bool RestarCostoJugada()
    {
        if (juegoTerminado) return false;

        if (monedasActuales >= costePorJugada)
        {
            monedasActuales -= costePorJugada;
            ActualizarUI();
            VerificarEstado();
            return true;
        }

        Debug.LogWarning("❌ No hay suficientes monedas para jugar.");
        return false;
    }

    /// <summary>
    /// Añade una cantidad de monedas al ganar.
    /// </summary>
    public void AñadirPremio(int cantidad)
    {
        if (juegoTerminado) return;

        monedasActuales = Mathf.Min(monedasActuales + cantidad, monedasMaximas);
        ActualizarUI();
        VerificarEstado();
    }

    /// <summary>
    /// Actualiza el texto del contador (ejemplo: 25 / 100).
    /// </summary>
    private void ActualizarUI()
    {
        if (monedasTexto != null)
            monedasTexto.text = $"{monedasActuales} / {monedasMaximas}";
    }

    /// <summary>
    /// Verifica si el jugador ha ganado o perdido.
    /// </summary>
    private void VerificarEstado()
    {
        if (monedasActuales <= 0)
        {
            monedasActuales = 0;
            ActualizarUI();
            MostrarPantallaDerrota();
        }
        else if (monedasActuales >= monedasMaximas)
        {
            monedasActuales = monedasMaximas;
            ActualizarUI();
            MostrarPantallaVictoria();
        }
    }

    /// <summary>
    /// Muestra la pantalla de derrota y detiene el juego.
    /// </summary>
    private void MostrarPantallaDerrota()
    {
        juegoTerminado = true;
        Debug.Log("💀 Juego terminado: sin monedas.");
        if (pantallaDerrota != null) pantallaDerrota.SetActive(true);
    }

    /// <summary>
    /// Muestra la pantalla de victoria y detiene el juego.
    /// </summary>
    private void MostrarPantallaVictoria()
    {
        juegoTerminado = true;
        Debug.Log("🏆 ¡Victoria! Se alcanzó el objetivo de monedas.");
        if (pantallaVictoria != null) pantallaVictoria.SetActive(true);
    }

    /// <summary>
    /// Devuelve la cantidad actual de monedas (para otros scripts).
    /// </summary>
    public int ObtenerMonedas() => monedasActuales;

    /// <summary>
    /// Reinicia el contador y oculta las pantallas (por si reinicias el juego).
    /// </summary>
    public void Reiniciar()
    {
        monedasActuales = monedasIniciales;
        juegoTerminado = false;
        if (pantallaDerrota) pantallaDerrota.SetActive(false);
        if (pantallaVictoria) pantallaVictoria.SetActive(false);
        ActualizarUI();
    }
}