using UnityEngine;

public class AnimatorOffset : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string offsetParam = "Offset";

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();

        // Semilla distinta por instancia (mejor que Random a secas)
        int seed = GetInstanceID() ^ (int)(Time.realtimeSinceStartup * 1000);
        var rng = new System.Random(seed);
        float offset = (float)rng.NextDouble(); // 0..1

        animator.SetFloat(offsetParam, offset);
    }

    void Start()
    {
        // Fuerza a que aplique el offset ya
        animator.Update(0f);
    }
}
    