using UnityEngine;

public class SimpleFPSController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private bool cursorFree = false;

    void Start()
    {
        LockCursor();
    }

    void Update()
    {
        HandleCursorMode();

        if (!cursorFree)
        {
            Look();
        }

        Move();

        // Escape por seguridad
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
    }

    void HandleCursorMode()
    {
        // Mantener RMB → liberar cursor
        if (Input.GetMouseButtonDown(1))
        {
            UnlockCursor();
            cursorFree = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            LockCursor();
            cursorFree = false;
        }
    }

    void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * 100f * Time.deltaTime;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -80f, 80f);

        Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
