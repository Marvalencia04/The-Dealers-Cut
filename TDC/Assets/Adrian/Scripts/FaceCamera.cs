using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera cam;
    private float updateInterval = 0.1f; // Solo actualizar cada 100ms
    private float nextUpdate = 0f;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (Time.time < nextUpdate) return;
        
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.forward);
        
        nextUpdate = Time.time + updateInterval;
    }
}