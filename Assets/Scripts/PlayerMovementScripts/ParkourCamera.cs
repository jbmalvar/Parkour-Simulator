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

        // 1. Standard Mouse Look Logic...
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        yRotation += lookInput.x * mouseSensitivity;
        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 2. Position the Camera on the Head Bone
        // We no longer need to scale bones to zero! 
        // The Near Clip Plane of the BodyCamera handles visibility.
        transform.position = targetHeadBone.position;

        if (movementScript != null && movementScript.IsRolling)
        {
            // Professional "Damped" Roll Camera:
            // 1. Get the raw head position
            Vector3 headPos = targetHeadBone.position;
            
            // 2. Lock the Y position to the player's standing eye-level 
            // This stops the camera from following the animation's "arc"
            float standingEyeHeight = playerBody.position.y + 1.5f; // Adjust 1.5 to your eye level
            
            // 3. Smoothly transition to the roll height so it doesn't "snap"
            float currentY = Mathf.Lerp(transform.position.y, standingEyeHeight, Time.deltaTime * 10f);
            
            transform.position = new Vector3(headPos.x, currentY, headPos.z);

            // 4. Keep the rotation follow (this is what makes the roll feel immersive)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadBone.rotation * Quaternion.Euler(xRotation, 0f, 0f), Time.deltaTime * 15f);
        }
        else
        {
            // Standard walk/run tilt logic
            float targetTilt = 0;
            if (movementScript.IsWallRunning)
                targetTilt = movementScript.WallSide == 1 ? tiltAmount : -tiltAmount;
            
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        }
    }
}