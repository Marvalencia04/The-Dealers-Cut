using UnityEngine;

public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10,
    Jack = 11,
    Queen = 12,
    King = 13,
    Ace = 14
}

public class Card : MonoBehaviour
{
    [Header("Blackjack Data")]
    public Suit suit;
    public Rank rank;

    [HideInInspector] public DeckXR deck;
    [HideInInspector] public GameObject prefabReference;
    [HideInInspector] public CardSnapZone currentZone;
    [HideInInspector] public Vector3 originalScale;

    // Para saber cuándo se ha levantado de un slot
    [HideInInspector] public float lastGrabTime = -999f;

    [Header("Materiales / Visual")]
    [Tooltip("Renderers donde se cambiará el material al ocultar la carta. " +
             "Si está vacío, se usarán automáticamente todos los Renderers hijos.")]
    public Renderer[] renderersToSwap;

    [Tooltip("Material que se usa como 'Joker' para ocultar la carta.")]
    public Material jokerMaterial;

    // Guardamos los materiales originales para poder restaurarlos
    private Material[][] originalMaterials;
    private bool materialsCached = false;

    [HideInInspector] public bool isHidden = false;

    private void Awake()
    {
        // Escala original (ya lo tenías)
        originalScale = transform.localScale;

        // Si no has rellenado renderers a mano, pillamos todos los hijos
        if (renderersToSwap == null || renderersToSwap.Length == 0)
        {
            renderersToSwap = GetComponentsInChildren<Renderer>(true);
        }

        CacheOriginalMaterials();
    }

    private void CacheOriginalMaterials()
    {
        if (materialsCached) return;
        if (renderersToSwap == null || renderersToSwap.Length == 0) return;

        originalMaterials = new Material[renderersToSwap.Length][];
        for (int i = 0; i < renderersToSwap.Length; i++)
        {
            var r = renderersToSwap[i];
            if (r != null)
            {
                originalMaterials[i] = r.materials;
            }
        }

        materialsCached = true;
    }

    /// <summary>
    /// Oculta o muestra la carta cambiando el material.
    /// hide = true  -> pone el material Joker
    /// hide = false -> restaura los materiales originales
    /// </summary>
    public void SetHidden(bool hide)
    {
        if (renderersToSwap == null || renderersToSwap.Length == 0) return;

        CacheOriginalMaterials();

        if (hide)
        {
            if (jokerMaterial == null) return;

            // Cambiar TODOS los sub-materiales por el Joker
            for (int i = 0; i < renderersToSwap.Length; i++)
            {
                var r = renderersToSwap[i];
                if (r == null) continue;

                var mats = r.materials;
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = jokerMaterial;
                }
                r.materials = mats;
            }
        }
        else
        {
            if (originalMaterials == null) return;

            // Restaurar materiales originales
            for (int i = 0; i < renderersToSwap.Length; i++)
            {
                var r = renderersToSwap[i];
                if (r == null) continue;

                if (i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    r.materials = originalMaterials[i];
                }
            }
        }

        isHidden = hide;
    }

    // Valor del blackjack (figuras = 10, As = 11, resto número)
    public int GetBlackjackValue()
    {
        switch (rank)
        {
            case Rank.Jack:
            case Rank.Queen:
            case Rank.King:
                return 10;

            case Rank.Ace:
                return 11;

            default:
                return (int)rank;
        }
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}
