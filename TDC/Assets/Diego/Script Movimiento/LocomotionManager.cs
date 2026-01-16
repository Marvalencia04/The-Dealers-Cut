using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocomotionManager : MonoBehaviour
{
    [Header("Dropdown References")]
    [SerializeField] private TMP_Dropdown movementDropdown;
    [SerializeField] private TMP_Dropdown rotationDropdown;

    [Header("Movement GameObjects")]
    [SerializeField] private GameObject teleportation;
    [SerializeField] private GameObject move;

    [Header("Rotation GameObjects")]
    [SerializeField] private GameObject snapTurn;
    [SerializeField] private GameObject continuousTurn;

    private void Start()
    {
        SetupDropdowns();
        
        movementDropdown.onValueChanged.AddListener(OnMovementChanged);
        rotationDropdown.onValueChanged.AddListener(OnRotationChanged);
        
        OnMovementChanged(movementDropdown.value);
        OnRotationChanged(rotationDropdown.value);
    }

    private void SetupDropdowns()
    {
        movementDropdown.ClearOptions();
        movementDropdown.AddOptions(new System.Collections.Generic.List<string> 
        { 
            "Teletransporte", 
            "Movimiento Continuo" 
        });

        rotationDropdown.ClearOptions();
        rotationDropdown.AddOptions(new System.Collections.Generic.List<string> 
        { 
            "Giro por Ángulos (Snap)", 
            "Giro Continuo" 
        });
    }

    private void OnMovementChanged(int index)
    {
        switch (index)
        {
            case 0: // Teletransporte
                if (teleportation != null) teleportation.SetActive(true);
                if (move != null) move.SetActive(false);
                Debug.Log("Sistema de teletransporte activado");
                break;
                
            case 1: // Movimiento Continuo
                if (teleportation != null) teleportation.SetActive(false);
                if (move != null) move.SetActive(true);
                Debug.Log("Movimiento continuo activado");
                break;
        }
    }

    private void OnRotationChanged(int index)
    {
        switch (index)
        {
            case 0: // Giro por ángulos (Snap Turn)
                if (snapTurn != null) snapTurn.SetActive(true);
                if (continuousTurn != null) continuousTurn.SetActive(false);
                Debug.Log("Giro por ángulos (Snap Turn) activado");
                break;
                
            case 1: // Giro Continuo
                if (snapTurn != null) snapTurn.SetActive(false);
                if (continuousTurn != null) continuousTurn.SetActive(true);
                Debug.Log("Giro continuo activado");
                break;
        }
    }

    private void OnDestroy()
    {
        movementDropdown.onValueChanged.RemoveListener(OnMovementChanged);
        rotationDropdown.onValueChanged.RemoveListener(OnRotationChanged);
    }
}