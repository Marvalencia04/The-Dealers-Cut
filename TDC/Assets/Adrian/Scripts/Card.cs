using UnityEngine;

public enum Suit
{
    Hearts,      // Corazones
    Diamonds,    // Diamantes
    Clubs,       // Tréboles
    Spades       // Picas
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

    // Valor del blackjack (base)
    public int GetBlackjackValue()
    {
        switch (rank)
        {
            case Rank.Jack:
            case Rank.Queen:
            case Rank.King:
                return 10;

            case Rank.Ace:
                return 11; // luego la mano decidirá si baja a 1

            default:
                return (int)rank; // valores 2–10
        }
    }

    [HideInInspector] public DeckXR deck;
    [HideInInspector] public GameObject prefabReference;
    [HideInInspector] public CardSnapZone currentZone;
    [HideInInspector] public Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public override string ToString()
    {
        return $"{rank} of {suit}";
    }
}

