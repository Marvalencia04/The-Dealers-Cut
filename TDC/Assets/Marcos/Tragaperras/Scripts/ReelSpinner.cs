using System.Collections;
using UnityEngine;

/// <summary>
/// Controla el giro de un carrete de tragaperras.
/// Gira hasta un índice exacto, ajustado en casillas.
/// Pensado para 16 items (22.5º por casilla) pero configurable.
/// </summary>
public class ReelSpinner_M : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    // Eje de rotación del cilindro (normalmente Z)
    public Axis rotateAxis = Axis.Z;

    // Número de casillas (símbolos) que tiene el cilindro
    public int items = 16;

    // Ángulo que indica dónde está el 0 visual real del cilindro en Unity
    public float baseOffsetDeg = 176f;

    // Desplazamiento lógico de casillas para alinear visualmente el símbolo correcto
    public int frontOffsetSlots = 0;

    // Si el cilindro gira al revés marcamos esto en true
    public bool invertDirection = false;

    // Cuántas vueltas completas da antes de frenar (solo estético)
    public float spinsBeforeStop = 3f;

    // Duración total del giro
    public float spinDuration = 1.2f;

    // Curva de aceleración / frenado
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    float SlotAngle => 360f / Mathf.Max(1, items); // grados que ocupa una casilla
    Coroutine current;

    //Cosas de audio
    public AudioSource loopSourceShared;   // Asignar en UN carrete (o en varios; el primero que llegue se registra)
    public AudioClip clipSpinLoop;         // Clip de “giro” (debe ser loopeable; el AudioSource hará loop=true)

    // Un AudioSource LOCAL para el one-shot de parada (puede ser el mismo en todos los carretes)
    public AudioSource sfxSourceStopLocal;
    public AudioClip clipReelStop;         // Clip “CilindroParar”

    // Estado global para el loop compartido
    static int __globalSpinningCount = 0;
    static AudioSource __globalLoopSource = null;
    static AudioClip __globalLoopClip = null;

    // Estado local para no desbalancear el contador si se interrumpe una corrutina
    bool _audioCounted = false;

    public float extraStopDelay = 0f; // segundos a esperar antes de encajar y sonar la parada

    [Header("Escalonado natural")]
    public float extraSpinTime = 0f;   // Segundos extra añadidos a spinDuration
    public float durationScale = 1f;   // Multiplicador de duración (1 = sin cambios)
    public bool IsSpinning => current != null;
    // Llama a esto para que gire hacia ese índice final
    public void SpinToIndex(int index)
    {
        index = Mod(index, items);



        if (current != null) StopCoroutine(current);
        current = StartCoroutine(SpinToIndexCo(index));
    }

    // Coloca directamente en el índice sin animación (para test)
    public void SetInstantToIndex(int index)
    {
        float target = IndexToAngle(index);
        SetLocalAngleDeg(Normalize360(target));
    }


    void Awake()
    {
        // Registrar (lazy) una única fuente global de loop
        if (__globalLoopSource == null && loopSourceShared != null)
        {
            __globalLoopSource = loopSourceShared;
            __globalLoopSource.loop = true;
            __globalLoopSource.playOnAwake = false;
        }
        if (__globalLoopClip == null && clipSpinLoop != null)
            __globalLoopClip = clipSpinLoop;
    }


    // Devuelve el índice aproximado actual en el que está el cilindro
    public int GetApproxIndex()
    {
        float ang = Normalize360(GetLocalAngleDeg() - baseOffsetDeg);

        if (invertDirection)
            ang = Normalize360(-ang);

        int idx = Mathf.RoundToInt(ang / SlotAngle) - frontOffsetSlots;

        return Mod(idx, items);
    }

    IEnumerator SpinToIndexCo(int index)
    {
        // --- AUDIO: al pasar de 0→1 carretes activos, arranca el loop compartido ---
        if (__globalSpinningCount == 0 && __globalLoopSource != null && __globalLoopClip != null)
        {
            __globalLoopSource.clip = __globalLoopClip;
            __globalLoopSource.Play();
        }
        __globalSpinningCount++;
        _audioCounted = true; // marcamos que este carrete ha incrementado el contador

        // --- Cálculo de giro ---
        float start = GetLocalAngleDeg();
        float target = IndexToAngle(index);

        // Avance total = vueltas estéticas + arco mínimo positivo hasta el target
        float totalDelta = spinsBeforeStop * 360f + ShortestPositiveArc(start, target);
        float end = start + totalDelta;

        float t = 0f;
        float dur = Mathf.Max(0.05f, (spinDuration * durationScale) + extraSpinTime);


        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            float ang = Mathf.LerpUnclamped(start, end, k);
            SetLocalAngleDeg(ang);
            yield
            return null;
        }

        if (extraStopDelay > 0f)
            yield return new WaitForSeconds(extraStopDelay);

        // Snap final exacto a la casilla
        SetLocalAngleDeg(Normalize360(target));
        current = null;

        // --- AUDIO: dispara el one-shot de parada ---
        if (sfxSourceStopLocal != null && clipReelStop != null)
            sfxSourceStopLocal.PlayOneShot(clipReelStop);

        // --- AUDIO: decrementa y, si ya no queda ningún carrete girando, para el loop ---
        __globalSpinningCount = Mathf.Max(0, __globalSpinningCount - 1);
        if (__globalSpinningCount == 0 && __globalLoopSource != null && __globalLoopSource.isPlaying)
            __globalLoopSource.Stop();

        _audioCounted = false; // este carrete ya no cuenta como “girando”
    }


    // Convierte índice → ángulo final exacto
    float IndexToAngle(int index)
    {
        int logical = invertDirection ? -index : index;
        float deg = baseOffsetDeg + (logical + frontOffsetSlots) * SlotAngle;
        return Normalize360(deg);
    }

    // ==============================================================
    // Utilidades internas
    // ==============================================================

    float GetLocalAngleDeg()
    {
        var e = transform.localEulerAngles;
        return rotateAxis switch
        {
            Axis.X => e.x,
            Axis.Y => e.y,
            _ => e.z
        };
    }

    void SetLocalAngleDeg(float deg)
    {
        var e = transform.localEulerAngles;
        switch (rotateAxis)
        {
            case Axis.X: e.x = deg; break;
            case Axis.Y: e.y = deg; break;
            default: e.z = deg; break;
        }
        transform.localEulerAngles = e;
    }

    static float Normalize360(float a) => Mathf.Repeat(a, 360f);

    static float Normalize180(float a)
    {
        a = Mathf.Repeat(a + 180f, 360f) - 180f;
        return a;
    }

    // devuelve el avance mínimo positivo para ir desde from hacia to sin retroceder
    static float ShortestPositiveArc(float fromDeg, float toDeg)
    {
        float delta = Normalize180(toDeg - fromDeg);
        if (delta < 0) delta += 360f;
        return delta;
    }

    // módulo positivo
    static int Mod(int a, int m) => (a % m + m) % m;
}
