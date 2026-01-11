using UnityEngine;

public class SlotLeverVR : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private Giro giro;

    [Header("Animación")]
    [SerializeField] private string triggerPull = "Pull";

    private bool isBusy = false;

    private void Awake()
    {
        if (leverAnimator == null)
            leverAnimator = GetComponent<Animator>();
    }

    // ==========================
    // 🔘 XR ACTIVATE
    // ==========================
    public void OnActivate()
    {
        if (isBusy) return;

        isBusy = true;
        leverAnimator.SetTrigger(triggerPull);

        Debug.Log("🕹 Palanca activada");
    }

    // ==========================
    // 🎰 EVENTO DE ANIMACIÓN
    // ==========================
    public void OnLeverAnimationFinished()
    {
        Debug.Log("🎰 Animación terminada → giro");

        if (giro != null)
            giro.IntentarGiro();

        isBusy = false;
    }
}
