using UnityEngine;

public class LineaLecturaRaycast : MonoBehaviour
{
    public Transform rodillo;
    public float distancia = 2f; // distancia hasta el cilindro
    public LayerMask capaRodillo;

    private RaycastHit hit;

    void Update()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, distancia, capaRodillo))
        {
            // Puedes usar el punto de impacto para saber la rotación actual
            float angulo = rodillo.localEulerAngles.x % 360f;
            Debug.DrawRay(transform.position, transform.forward * distancia, Color.green);

            // Determinar símbolo según el ángulo
            Simbolo simbolo = CalcularSimboloSegunAngulo(angulo);
            Debug.Log("Símbolo detectado: " + simbolo);
        }
    }

    private Simbolo CalcularSimboloSegunAngulo(float angulo)
    {
        float gradosPorCara = 360f / 16f; // 22.5° por cara
        int indice = Mathf.FloorToInt(angulo / gradosPorCara);
        // Mapea los 16 índices a tus símbolos reales
        switch (indice % 4)
        {
            case 0: return Simbolo.BAR;
            case 1: return Simbolo.Campana;
            case 2: return Simbolo.Cereza;
            case 3: return Simbolo.Siete;
        }
        return Simbolo.BAR;
    }
}
