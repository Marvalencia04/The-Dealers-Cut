using UnityEngine;

/// <summary>
/// Controla la palanca del slot en VR.
/// Se activa por rayo VR, reproduce animación
/// y lanza el giro al terminar.
/// </summary>
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

    // ==========================================================
    // 🎯 LLAMADO DESDE EL RAYO VR
    // ==========================================================
    public void OnRayInteract()
    {
        if (isBusy) return;

        isBusy = true;
        leverAnimator.SetTrigger(triggerPull);

        Debug.Log("🕹 Palanca activada (animación)");
    }

    // ==========================================================
    // 🎰 LLAMADO DESDE LA ANIMACIÓN (Animation Event)
    // ==========================================================
    public void OnLeverAnimationFinished()
    {
        Debug.Log("🎰 Animación terminada → iniciar giro");

        if (giro != null)
            giro.IntentarGiro();

        isBusy = false;
    }
}
