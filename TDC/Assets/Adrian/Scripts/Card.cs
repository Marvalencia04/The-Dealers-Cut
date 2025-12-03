using UnityEngine;

public class Card : MonoBehaviour
{
    [HideInInspector] public DeckXR deck;
    [HideInInspector] public GameObject prefabReference;
    [HideInInspector] public CardSnapZone currentZone;
    [HideInInspector] public Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }
}
