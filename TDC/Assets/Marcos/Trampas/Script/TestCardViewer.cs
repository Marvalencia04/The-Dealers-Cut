using UnityEngine;

public class TestCardViewer : MonoBehaviour
{
    [Header("Asignar cartas manualmente")]
    public GameObject carta1;
    public GameObject carta2;
    public GameObject carta3;

    [Header("Posiciones donde aparecerán")]
    public Transform slot1;
    public Transform slot2;
    public Transform slot3;

    [Header("Opciones")]
    public float escala = 0.02f;   // Escala para que no sean gigantes
    public bool pintarRojo = false;

    void Start()
    {
        MostrarCarta(carta1, slot1, 1);
        MostrarCarta(carta2, slot2, 2);
        MostrarCarta(carta3, slot3, 3);
    }

    void MostrarCarta(GameObject prefab, Transform slot, int numero)
    {
        if (prefab == null || slot == null)
        {
            Debug.LogWarning($"[TestCardViewer] Carta {numero}: Falta prefab o slot.");
            return;
        }

        Debug.Log($"[TestCardViewer] Instanciando carta {numero}: {prefab.name}");

        GameObject inst = Instantiate(prefab, slot.position, slot.rotation);
        inst.transform.localScale = Vector3.one * escala;

        Renderer r = inst.GetComponentInChildren<Renderer>();
        if (r != null)
        {
            Debug.Log($"[TestCardViewer] Renderer encontrado en carta {numero}.");
            if (pintarRojo) r.material.color = Color.red;
        }
        else
        {
            Debug.LogWarning($"[TestCardViewer] NO se encontró Renderer en carta {numero}.");
        }
    }
}
