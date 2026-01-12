using UnityEngine;
using TMPro;

public class MusicDropdown : MonoBehaviour
{
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private TMP_Dropdown dropdown;

    private void Start()
    {
        if (!dropdown)
            dropdown = GetComponent<TMP_Dropdown>();

        dropdown.onValueChanged.AddListener(OnDropdownChanged);

        // Forzar música correcta al arrancar
        OnDropdownChanged(dropdown.value);
    }

    private void OnDropdownChanged(int value)
    {
        if (!musicManager) return;

        if (value == 0)
            musicManager.SetGenre(MusicGenre.Jazz);
        else if (value == 1)
            musicManager.SetGenre(MusicGenre.Forties);
    }
}
