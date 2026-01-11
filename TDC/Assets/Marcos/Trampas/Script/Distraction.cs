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

    [Header("Indicadores visuales")]
    public GameObject exclamationPrefab; // Prefab de la exclamación
    public Transform[] exclamationPositions; // Deben ser 3

    private Dictionary<CardSnapZone, int> initialCounts = new Dictionary<CardSnapZone, int>();
    private Coroutine distractionCoroutine;
    private List<GameObject> activeExclamations = new List<GameObject>();

    // ==========================
    // INICIO DE LA TRAMPA
    // ==========================
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

        SpawnExclamations(); // Aparece indicador visual

        distractionCoroutine = StartCoroutine(DistractionTimer());
    }

    // ==========================
    // COROUTINE DEL TEMPORIZADOR
    // ==========================
    private IEnumerator DistractionTimer()
    {
        float elapsed = 0f;

        while (elapsed < distractionTime)
        {
            elapsed += Time.deltaTime;

            if (timerText != null)
                timerText.text = $"Tiempo: {Mathf.Ceil(distractionTime - elapsed)}";

            yield return null;
        }

        EndDistraction();
    }

    // ==========================
    // CANCELAR TRAMPA
    // ==========================
    public void CancelDistraction()
    {
        if (distractionCoroutine != null)
            StopCoroutine(distractionCoroutine);

        CompleteDistraction();
    }

    // ==========================
    // FINALIZAR TRAMPA
    // ==========================
    private void EndDistraction()
    {
        CompleteDistraction();
    }

    // ==========================
    // CÓDIGO COMÚN PARA FINALIZAR
    // ==========================
    private void CompleteDistraction()
    {
        if (distractionMenu != null)
            distractionMenu.SetActive(false);

        DestroyExclamations(); // Eliminar indicadores

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

    // ==========================
    // VISUAL: EXCLAMACIONES
    // ==========================
    private void SpawnExclamations()
    {
        if (exclamationPrefab == null || exclamationPositions == null) return;

        DestroyExclamations(); // Limpiar por si quedaba alguna

        foreach (var pos in exclamationPositions)
        {
            if (pos == null) continue;

            GameObject exclamation = Instantiate(exclamationPrefab, pos.position, pos.rotation, pos);
            activeExclamations.Add(exclamation);
        }
    }

    private void DestroyExclamations()
    {
        foreach (var e in activeExclamations)
        {
            if (e != null)
                Destroy(e);
        }
        activeExclamations.Clear();
    }
}
