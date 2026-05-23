using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody; // Assign your "Parent" object here
    
    [Header("Settings")]
    public float mouseSensitivity = 15f;
    public float verticalClamp = 85f; // 85 degrees lets you see your feet without clipping

    [HideInInspector] public float xRotation = 0f;
    private InputAction lookAction;

    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (lookAction == null || playerBody == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // 1. Horizontal Rotation (Rotate the body)
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        playerBody.Rotate(Vector3.up * mouseX);

        // 2. Vertical Rotation (Calculate pitch)
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        // Apply pitch to this camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}