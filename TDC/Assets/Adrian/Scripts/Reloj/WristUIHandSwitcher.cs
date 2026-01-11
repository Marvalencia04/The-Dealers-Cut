using UnityEngine;
using UnityEngine.InputSystem;

public class WristUIHandSwitcher : MonoBehaviour
{
    [Header("Canvas en cada mano")]
    public GameObject canvasIzquierda;
    public GameObject canvasDerecha;

    [Header("Input Actions (igual que Grip Action)")]
    public InputActionProperty botonY; // Left Hand - Y
    public InputActionProperty botonB; // Right Hand - B

    private bool visible = false;

    private enum Mano { Izquierda, Derecha }
    private Mano manoActiva = Mano.Izquierda;

    private void OnEnable()
    {
        botonY.action.performed += OnBotonY;
        botonB.action.performed += OnBotonB;

        botonY.action.Enable();
        botonB.action.Enable();

        ActualizarCanvas();
    }

    private void OnDisable()
    {
        botonY.action.performed -= OnBotonY;
        botonB.action.performed -= OnBotonB;
    }

    private void OnBotonY(InputAction.CallbackContext ctx)
    {
        CambiarMano(Mano.Izquierda);
    }

    private void OnBotonB(InputAction.CallbackContext ctx)
    {
        CambiarMano(Mano.Derecha);
    }

    private void CambiarMano(Mano mano)
    {
        if (manoActiva == mano)
        {
            visible = !visible; // toggle
        }
        else
        {
            manoActiva = mano;
            visible = true;
        }

        ActualizarCanvas();
    }

    private void ActualizarCanvas()
    {
        canvasIzquierda.SetActive(visible && manoActiva == Mano.Izquierda);
        canvasDerecha.SetActive(visible && manoActiva == Mano.Derecha);
    }
}
