using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SlotLeverVR : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator leverAnimator;
    [SerializeField] private Giro giro;

    private XRBaseInteractor currentInteractor;
    private bool isBusy = false;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        if (leverAnimator == null)
            leverAnimator = GetComponentInChildren<Animator>();

        interactable = GetComponent<XRBaseInteractable>();
    }

    // =================================
    // DEBUG: teclado (opcional)
    // =================================
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("⌨ SPACE → Pull");
            TryPull();
        }*/
    }

    // =================================
    // XR Select Entered
    // =================================
    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        currentInteractor = args.interactorObject as XRBaseInteractor;
        TryPull();
    }

    // =================================
    // Lógica central de activación
    // =================================
    private void TryPull()
    {

        isBusy = true;

        leverAnimator.SetTrigger("Pull");

        Debug.Log("🕹 Palanca activada");
    }

    // =================================
    // EVENTO DE ANIMACIÓN (FINAL)
    // =================================
    public void OnLeverAnimationFinished()
    {
        Debug.Log("🎰 Animación terminada → iniciar giro");

        if (giro != null)
            giro.IntentarGiro();

        ForceDeselect();

    }

    // =================================
    // Forzar liberación XR
    // =================================
    private void ForceDeselect()
    {
        if (currentInteractor == null || interactable == null)
            return;

        currentInteractor.interactionManager.SelectExit(
            (IXRSelectInteractor)currentInteractor,
            (IXRSelectInteractable)interactable
        );

        currentInteractor = null;

        Debug.Log("🔓 Palanca liberada");
    }
}
