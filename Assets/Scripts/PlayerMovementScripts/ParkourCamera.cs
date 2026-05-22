using UnityEngine;
using UnityEngine.InputSystem;

public class ParkourCamera : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    public Transform targetHeadBone;
    public Transform targetNeckBone;
    
    [Header("Look Settings")]
    // NOTE: Lower this in the Inspector! (e.g., to 0.1 - 0.5) 
    // Since we removed Time.deltaTime, the old value of 50 will be way too fast.
    public float mouseSensitivity = 0.2f; 
    public float maxLookUp = 85f;
    public float maxLookDown = 10f; 

    [Header("Stabilization")]
    public Vector3 headOffset = new Vector3(0, 0.05f, 0.15f);

    [Header("Wall Run Effects")]
    public float tiltAmount = 15f;
    public float tiltSpeed = 5f;

    private float xRotation = 0f;
    private float yRotation = 0f; // Explicitly track Y rotation to prevent drift
    private float currentTilt = 0f;
    private InputAction lookAction;
    private PlayerMovement movementScript;

    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        movementScript = playerBody.GetComponent<PlayerMovement>();
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize Y rotation to match the body's starting rotation
        if (playerBody != null)
        {
            yRotation = playerBody.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (lookAction == null || playerBody == null || targetHeadBone == null) return;

        // 1. Get Mouse Input
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        // 2. Calculate Rotations
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);

        // 3. Rotate the Player Body (Horizontal only)
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 4. Calculate Wall Run Tilt
        float targetTilt = 0;
        if (movementScript != null && movementScript.IsWallRunning)
        {
            targetTilt = movementScript.WallSide == 1 ? tiltAmount : -tiltAmount;
        }
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        if (movementScript != null && movementScript.IsRolling)
        {
            // Hide ONLY head and neck to keep the camera height stable
            if (targetHeadBone != null) targetHeadBone.localScale = Vector3.zero;
            if (targetNeckBone != null) targetNeckBone.localScale = Vector3.zero;

            // Use the bone's position (this will be correct as long as its PARENTS are scale 1)
            transform.position = targetHeadBone.position; 
            transform.rotation = targetHeadBone.rotation * Quaternion.Euler(xRotation, 0f, 0f);
        }
        else
        {
            // Restore scale
            if (targetHeadBone != null) targetHeadBone.localScale = Vector3.one;
            if (targetNeckBone != null) targetNeckBone.localScale = Vector3.one;

            transform.position = targetHeadBone.position + 
                                playerBody.right * headOffset.x + 
                                playerBody.up * headOffset.y + 
                                playerBody.forward * headOffset.z;

            transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        }
    }
}