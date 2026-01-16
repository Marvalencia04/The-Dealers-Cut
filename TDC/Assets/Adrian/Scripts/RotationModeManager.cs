using UnityEngine;
using TMPro;

public class RotationModeManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown rotationDropdown;
    [SerializeField] private Behaviour snapTurnProvider;
    [SerializeField] private Behaviour continuousTurnProvider;

    private void Start()
    {
        rotationDropdown.onValueChanged.AddListener(_ => Apply());
        Apply();
    }

    private void Apply()
    {
        string option = rotationDropdown.options[rotationDropdown.value].text
            .Trim()
            .ToLowerInvariant();

        bool snap = option.Contains("snap");

        snapTurnProvider.enabled = snap;
        continuousTurnProvider.enabled = !snap;
    }
}
