using UnityEngine;

public class BlackjackInteractionGateXR : MonoBehaviour
{
    [Header("World References")]
    [SerializeField] private DeckXR deck;
    [SerializeField] private CardCollector[] collectors;


    [Header("Snap Zones")]
    [SerializeField] private CardSnapZone dealerZone;
    [SerializeField] private CardSnapZone[] playerZones;

    [Header("Optional buttons/objects per phase")]
    [SerializeField] private GameObject revealDealerButton;
    [SerializeField] private GameObject resolveResultsButton;
    [SerializeField] private GameObject nextPhaseButton;

    public void ApplyPhase(BlackjackPhase phase)
    {
        // Defaults: safe
        SetDeck(false);
        SetCollect(false);
        SetAllZonesReadOnly(true);
        SetAllZonesCanReceive(false);
        SetAllZonesCanGrab(false);

        SetActive(revealDealerButton, false);
        SetActive(resolveResultsButton, false);
        SetActive(nextPhaseButton, true); // normalmente siempre visible

        switch (phase)
        {
            case BlackjackPhase.Apuestas:
                // Aquí normalmente no quieres cartas aún
                // pero puedes dejar al dealer preparar cosas.
                SetDeck(false);
                SetCollect(false);
                break;

            case BlackjackPhase.Reparto:
                // Dealer reparte manual: habilitamos sacar cartas y soltarlas en zonas
                SetDeck(true);
                SetAllZonesReadOnly(false);
                SetAllZonesCanReceive(true);
                SetAllZonesCanGrab(true); // para recolocar si cae mal
                break;

            case BlackjackPhase.TurnoJugadores:
                SetDeck(true);

                // Dejar que BlackjackTable controle por zona
                SetAllZonesReadOnly(false);

                // No mover cartas existentes
                SetAllZonesCanGrab(false);

                // No forzamos canReceive global
                break;


            case BlackjackPhase.RevelarSegundaCarta:
                // Mostrar botón/acción de revelar
                SetActive(revealDealerButton, true);
                break;

            case BlackjackPhase.TurnoDealer:
                // Dealer juega manual: habilitar deck, permitir coger cartas
                SetDeck(true);
                SetAllZonesReadOnly(false);
                SetAllZonesCanReceive(true);
                SetAllZonesCanGrab(true);
                break;

            case BlackjackPhase.Resultados:
                // Bloquear cartas y mostrar botón para resolver
                SetDeck(false);
                SetAllZonesCanGrab(false);
                SetActive(resolveResultsButton, true);

                // y permitir recoger cuando el jugador quiera limpiar mesa
                SetCollect(true);
                break;
        }
    }

    private void SetDeck(bool v)
    {
        if (deck != null) deck.SetCanDrawCards(v);
    }

    private void SetCollect(bool canCollect)
    {
        if (collectors == null) return;
        foreach (var c in collectors)
        {
            if (c != null)
                c.SetCanCollect(canCollect);
        }
    }


    private void SetAllZonesReadOnly(bool v)
    {
        if (dealerZone != null) dealerZone.SetReadOnly(v);
        if (playerZones == null) return;
        foreach (var z in playerZones) if (z != null) z.SetReadOnly(v);
    }

    private void SetAllZonesCanReceive(bool v)
    {
        if (dealerZone != null) dealerZone.SetCanReceiveNewCards(v);
        if (playerZones == null) return;
        foreach (var z in playerZones) if (z != null) z.SetCanReceiveNewCards(v);
    }

    private void SetAllZonesCanGrab(bool v)
    {
        if (dealerZone != null) dealerZone.SetCanGrabFromZone(v);
        if (playerZones == null) return;
        foreach (var z in playerZones) if (z != null) z.SetCanGrabFromZone(v);
    }

    private void SetActive(GameObject go, bool v)
    {
        if (go != null) go.SetActive(v);
    }
}
