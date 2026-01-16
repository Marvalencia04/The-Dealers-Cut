using TMPro;
using UnityEngine;

/// <summary>
/// Controlador de la l骻ica de monedas y su visualizaci髇.
/// </summary>
public class CurrencyController : MonoBehaviour
{
    [Header("Configuraci髇")]
    public SlotCurrencyManager currency = new SlotCurrencyManager();

    [Header("UI (opcional)")]
    public TextMeshProUGUI coinsText;

    private void Start()
    {
        ActualizarUI(currency.ObtenerMonedas());
    }

    void ActualizarUI(int nuevaCantidad)
    {
        if (coinsText != null)
            coinsText.text = nuevaCantidad.ToString("N0");
    }

    public bool IntentarRestarCosto()
    {
        return currency.RestarCostoJugada();
    }

    public void AñadirPremio(int cantidad)
    {
        currency.AñadirPremio(cantidad);
    }

    public int ObtenerMonedas()
    {
        return currency.ObtenerMonedas();
    }
}
