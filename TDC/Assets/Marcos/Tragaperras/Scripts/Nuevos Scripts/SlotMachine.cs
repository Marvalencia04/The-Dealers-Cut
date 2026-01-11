using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Tipos de figuras de la tragaperras.
/// </summary>
public enum SlotFigure
{
    Triple7,
    Campana,
    Cereza,
    Bar
}

/// <summary>
/// Controlador simplificado de la tragaperras.
/// La figura determina directamente qué trampa se otorga.
/// </summary>
public class SlotMachine : MonoBehaviour
{
    [Header("Configuración de tiempo")]
    public float spinTime = 2f;

    [Header("UI de premio")]
    public GameObject prizeScreen;
    public Image prizeIcon;
    public TextMeshProUGUI prizeText;

    [Header("Iconos de trampas (opcional)")]
    public Sprite iconMazoVisible;
    public Sprite iconCartaAElegir;
    public Sprite iconMiraAlli;
    public Sprite iconLlamadaSeguridad;

    private bool isSpinning = false;

    /// <summary>
    /// Llamado al tirar de la palanca
    /// </summary>
    public void PullLever()
    {
        if (isSpinning) return;
        StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SpinCoroutine()
    {
        isSpinning = true;

        Debug.Log("🎰 La máquina gira...");
        yield return new WaitForSeconds(spinTime);

        // Elegir figura aleatoria según pesos (esto puede venir del sistema actual de la tragaperras)
        SlotFigure figura = GetRandomFigure();

        // Determinar la trampa y rareza según la figura
        (TrapType trap, TrapRarity rarity, Sprite icon) = GetTrapForFigure(figura);

        // Sumar +1 uso en TrampasManager
        if (TrampasManager.Instance != null)
        {
            TrampasManager.Instance.RecoverTrap(rarity);
        }

        // Mostrar UI
        if (prizeScreen != null) prizeScreen.SetActive(true);
        if (prizeText != null) prizeText.text = $"¡Premio! {trap} ({rarity})";
        if (prizeIcon != null && icon != null) prizeIcon.sprite = icon;

        Debug.Log($"🎉 Figura: {figura} → Trampa: {trap} | Rareza: {rarity}");

        isSpinning = false;
    }

    /// <summary>
    /// Cierra la pantalla de premio
    /// </summary>
    public void ClosePrizeScreen()
    {
        if (prizeScreen != null)
            prizeScreen.SetActive(false);
    }

    /// <summary>
    /// Simula la aleatorización de la figura
    /// </summary>
    private SlotFigure GetRandomFigure()
    {
        // Aquí puedes poner la lógica de pesos que ya tenías
        int r = Random.Range(0, 4);
        return (SlotFigure)r;
    }

    /// <summary>
    /// Asigna la trampa según la figura
    /// </summary>
    private (TrapType trap, TrapRarity rarity, Sprite icon) GetTrapForFigure(SlotFigure figura)
    {
        switch (figura)
        {
            case SlotFigure.Triple7:
                return (TrapType.LlamadaSeguridad, TrapRarity.Legendaria, iconLlamadaSeguridad);

            case SlotFigure.Campana:
                return (TrapType.MiraAlli, TrapRarity.Epica, iconMiraAlli);

            case SlotFigure.Cereza:
                return (TrapType.CartaAElegir, TrapRarity.Rara, iconCartaAElegir);

            case SlotFigure.Bar:
                return (TrapType.MazoVisible, TrapRarity.Comun, iconMazoVisible);

            default:
                return (TrapType.MazoVisible, TrapRarity.Comun, iconMazoVisible);
        }
    }
}
