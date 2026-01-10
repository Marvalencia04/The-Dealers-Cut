using UnityEngine;

[System.Serializable]
public class BlackjackNPC
{
    public int Id;
    public string DisplayName;

    // Apuesta de la ronda actual
    public int CurrentBet { get; private set; }

    // Estado de ronda
    public bool HasStood { get; private set; }
    public bool IsRemovedBySecurity { get; private set; } // Trampa "Llamada de seguridad"

    // Perfil simple de riesgo (0 = conservador, 1 = agresivo)
    public float Risk;

    public BlackjackNPC(int id, string name, float risk)
    {
        Id = id;
        DisplayName = name;
        Risk = Mathf.Clamp01(risk);
        ResetForNewRound();
    }

    public void ResetForNewRound()
    {
        CurrentBet = 0;
        HasStood = false;
        IsRemovedBySecurity = false;
    }

    public void PlaceBet(int amount)
    {
        CurrentBet = Mathf.Max(0, amount);
    }

    public void MarkStand()
    {
        HasStood = true;
    }

    public void RemoveBySecurity()
    {
        IsRemovedBySecurity = true;
        HasStood = true; // no juega más esta ronda
    }
}
