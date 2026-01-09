using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // Solo si quieres mostrar temporizador en UI

public class Distraction : MonoBehaviour
{
    [Header("Configuración")]
    public float distractionTime = 5f;

    [Header("UI")]
    public GameObject distractionMenu;
    public TMP_Text timerText; // opcional, para mostrar cuenta atrás

    [Header("Cartas")]
    public List<CardSnapZone> allZones;

    private Dictionary<CardSnapZone, int> initialCounts = new Dictionary<CardSnapZone, int>();
    private Coroutine distractionCoroutine;

    public void StartDistraction()
    {
        if (distractionCoroutine != null)
            StopCoroutine(distractionCoroutine);

        initialCounts.Clear();
        foreach (var zone in allZones)
        {
            if (zone != null)
                initialCounts[zone] = zone.GetOccupiedCount();
        }

        if (distractionMenu != null)
            distractionMenu.SetActive(true);

        foreach (var zone in allZones)
        {
            if (zone != null)
                zone.SetCanGrabFromZone(true);
        }

        distractionCoroutine = StartCoroutine(DistractionTimer());
    }

    private IEnumerator DistractionTimer()
    {
        float elapsed = 0f;

        while (elapsed < distractionTime)
        {
            elapsed += Time.deltaTime;

            // Mostrar temporizador en UI
            if (timerText != null)
                timerText.text = $"Tiempo: {Mathf.Ceil(distractionTime - elapsed)}";

            yield return null;
        }

        EndDistraction();
    }

    private void EndDistraction()
    {
        CompleteDistraction();
    }

    public void CancelDistraction()
    {
        if (distractionCoroutine != null)
            StopCoroutine(distractionCoroutine);

        CompleteDistraction();
    }

    /// <summary>
    /// Código común para finalizar la distracción
    /// </summary>
    private void CompleteDistraction()
    {
        if (distractionMenu != null)
            distractionMenu.SetActive(false);

        bool allCorrect = true;

        foreach (var zone in allZones)
        {
            if (zone == null) continue;

            int initial = initialCounts.ContainsKey(zone) ? initialCounts[zone] : 0;
            int current = zone.GetOccupiedCount();

            if (current != initial)
            {
                allCorrect = false;
                Debug.LogWarning($"{zone.name} tiene un número incorrecto de cartas: tiene {current}, debería tener {initial}");
            }

            // Bloquear interacción de cartas
            zone.SetCanGrabFromZone(false);
        }

        if (allCorrect)
            Debug.Log("Todo Correcto");
    }
}
