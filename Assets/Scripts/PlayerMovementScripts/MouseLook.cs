using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("References")]
    public Transform headTransform;
    
    [Header("Settings")]
    public float mouseSensitivity = 20f;
    public float verticalClamp = 80f;

    private float xRotation = 0f;
    private InputAction lookAction;

    void Start()
    {
        // Find the "Look" action from your project-wide Input Actions asset
        lookAction = InputSystem.actions.FindAction("Look");
        
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // 1. Horizontal Rotation (Rotate the player body)
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);
    }

    void LateUpdate()
    {
        if (lookAction == null || headTransform == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        // 2. Vertical Rotation (Rotate the head bone)
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        // Apply the pitch rotation to the head
        headTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }
}