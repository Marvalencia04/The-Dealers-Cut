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

    [HideInInspector] public float lastGrabTime = -999f;

    [Header("Materiales / Visual")]
    [Tooltip("Renderers donde se cambiará el material al ocultar la carta.")]
    public Renderer[] renderersToSwap;

    [Tooltip("Material que se usa como 'Joker' para ocultar la carta.")]
    public Material jokerMaterial;

    private Material[][] originalMaterials;
    private bool materialsCached = false;

    [HideInInspector] public bool isHidden = false;

    // 🔥 OPTIMIZACIÓN: Cachear el valor de blackjack
    private int cachedBlackjackValue = -1;

    private void Awake()
    {
        originalScale = transform.localScale;

        if (renderersToSwap == null || renderersToSwap.Length == 0)
        {
            renderersToSwap = GetComponentsInChildren<Renderer>(true);
        }

        CacheOriginalMaterials();

        // 🔥 OPTIMIZACIÓN: Calcular el valor una sola vez
        cachedBlackjackValue = CalculateBlackjackValue();
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

    public void SetHidden(bool hide)
    {
        // 🔥 OPTIMIZACIÓN: No hacer nada si ya está en ese estado
        if (isHidden == hide)
            return;

        if (renderersToSwap == null || renderersToSwap.Length == 0) return;

        CacheOriginalMaterials();

        if (hide)
        {
            if (jokerMaterial == null) return;

            for (int i = 0; i < renderersToSwap.Length; i++)
            {
                var r = renderersToSwap[i];
                if (r == null) continue;

                // 🔥 OPTIMIZACIÓN: Reutilizar array en lugar de crear uno nuevo
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    mats[m] = jokerMaterial;
                }
                r.sharedMaterials = mats;
            }
        }
        else
        {
            if (originalMaterials == null) return;

            for (int i = 0; i < renderersToSwap.Length; i++)
            {
                var r = renderersToSwap[i];
                if (r == null) continue;

                if (i < originalMaterials.Length && originalMaterials[i] != null)
                {
                    r.sharedMaterials = originalMaterials[i];
                }
            }
        }

        isHidden = hide;
    }

    // 🔥 OPTIMIZACIÓN: Método privado para calcular el valor
    private int CalculateBlackjackValue()
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

    // 🔥 OPTIMIZACIÓN: Devolver valor cacheado
    public int GetBlackjackValue()
    {
        return cachedBlackjackValue;
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}