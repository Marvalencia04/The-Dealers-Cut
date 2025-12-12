using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.forward);
    }
}
