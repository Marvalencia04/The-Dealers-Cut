using UnityEngine;
using TMPro;

public class LocomotionDebugger : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown movementDropdown;
    [SerializeField] private TMP_Dropdown rotationDropdown;
    [SerializeField] private GameObject teleportation;
    [SerializeField] private GameObject move;
    [SerializeField] private GameObject snapTurn;
    [SerializeField] private GameObject continuousTurn;

    private void Start()
    {
        Debug.Log("=== LOCOMOTION DEBUGGER INICIADO ===");
        
        // Verificar dropdowns
        if (movementDropdown == null)
            Debug.LogError("❌ Movement Dropdown NO está asignado!");
        else
            Debug.Log("✅ Movement Dropdown asignado correctamente");
            
        if (rotationDropdown == null)
            Debug.LogError("❌ Rotation Dropdown NO está asignado!");
        else
            Debug.Log("✅ Rotation Dropdown asignado correctamente");
        
        // Verificar GameObjects de movimiento
        if (teleportation == null)
            Debug.LogError("❌ Teleportation NO está asignado!");
        else
            Debug.Log($"✅ Teleportation asignado: {teleportation.name} - Activo: {teleportation.activeSelf}");
            
        if (move == null)
            Debug.LogError("❌ Move NO está asignado!");
        else
            Debug.Log($"✅ Move asignado: {move.name} - Activo: {move.activeSelf}");
        
        // Verificar GameObjects de rotación
        if (snapTurn == null)
            Debug.LogError("❌ Snap Turn NO está asignado!");
        else
            Debug.Log($"✅ Snap Turn asignado: {snapTurn.name} - Activo: {snapTurn.activeSelf}");
            
        if (continuousTurn == null)
            Debug.LogError("❌ Continuous Turn NO está asignado!");
        else
            Debug.Log($"✅ Continuous Turn asignado: {continuousTurn.name} - Activo: {continuousTurn.activeSelf}");
        
        Debug.Log("=== FIN DEL DIAGNÓSTICO ===");
    }
    
    private void Update()
    {
        // Prueba manual con teclas
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Debug.Log("Tecla 1: Activando Teleportation");
            if (teleportation != null) teleportation.SetActive(true);
            if (move != null) move.SetActive(false);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Debug.Log("Tecla 2: Activando Move");
            if (teleportation != null) teleportation.SetActive(false);
            if (move != null) move.SetActive(true);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Debug.Log("Tecla 3: Activando Snap Turn");
            if (snapTurn != null) snapTurn.SetActive(true);
            if (continuousTurn != null) continuousTurn.SetActive(false);
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Debug.Log("Tecla 4: Activando Continuous Turn");
            if (snapTurn != null) snapTurn.SetActive(false);
            if (continuousTurn != null) continuousTurn.SetActive(true);
        }
    }
}