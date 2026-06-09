using UnityEngine;
using UnityEngine.InputSystem;

public class ParkourCamera : MonoBehaviour
{
    [Header("References")]
    public Transform playerBody;
    public Transform targetHeadBone;
    public Transform targetNeckBone;
    
    [Header("Look Settings")]
    public float mouseSensitivity = 0.2f; 
    public float maxLookUp = 85f;
    public float maxLookDown = 40f; 

    [Header("Stabilization")]
    public Vector3 headOffset = new Vector3(0, 0.05f, 0.15f);

    [Header("Wall Run Effects")]
    public float tiltAmount = 15f;
    public float slideTilt = -20f;
    public float tiltSpeed = 5f;

    private float xRotation = 0f;
    private float yRotation = 0f; 
    private float currentTilt = 0f;
    private InputAction lookAction;
    private PlayerMovement movementScript;
    
    // Crouch Offset Variables
    private float currentYOffset = 0f;
    private float initialHeadHeight;

    void Start()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        movementScript = playerBody.GetComponent<PlayerMovement>();
        Cursor.lockState = CursorLockMode.Locked;

        if (playerBody != null)
        {
            yRotation = playerBody.eulerAngles.y;
            
            // Calculate how high the head bone is relative to the player's root
            if (targetHeadBone != null)
            {
                initialHeadHeight = targetHeadBone.position.y - playerBody.position.y;
            }
        }
    }

    void LateUpdate()
    {
        // =========================================================
        // PAUSE CHECK: If the game is paused, stop everything here.
        // =========================================================
        if (PauseMenu.GameIsPaused) return;

        if (lookAction == null || playerBody == null || targetHeadBone == null) return;

        // 1. Standard Mouse Look Logic
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        yRotation += lookInput.x * mouseSensitivity;
        xRotation -= lookInput.y * mouseSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // 2. Determine Camera Height Offset (Crouch / Slide logic without animations)
        float targetYOffset = 0f;
        
        if (movementScript != null)
        {
            // Drop camera by half its height if crouching or sliding
            if (movementScript.IsCrouching || movementScript.IsSliding)
            {
                targetYOffset = -(initialHeadHeight / 2f);
            }
        }

        // Smoothly transition the offset so it feels like a physical crouch
        currentYOffset = Mathf.Lerp(currentYOffset, targetYOffset, Time.deltaTime * 10f);

        // This uses the headOffset you already defined (0.15f forward) 
        // to keep the camera in front of the face while still following the head bone.
        transform.position = targetHeadBone.TransformPoint(headOffset) + new Vector3(0, currentYOffset, 0);

        if (movementScript != null && movementScript.IsRolling)
        {
            // Professional "Damped" Roll Camera:
            Vector3 headPos = targetHeadBone.position;
            float standingEyeHeight = playerBody.position.y + initialHeadHeight; 
            
            float currentY = Mathf.Lerp(transform.position.y, standingEyeHeight, Time.deltaTime * 10f);
            transform.position = new Vector3(headPos.x, currentY, headPos.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetHeadBone.rotation * Quaternion.Euler(xRotation, 0f, 0f), Time.deltaTime * 15f);
        }
        else
        {
            // Standard walk/run/crouch tilt logic
            float targetTilt = 0;
            if (movementScript.IsWallRunning)
            {
                targetTilt = movementScript.WallSide == 1 ? tiltAmount : -tiltAmount;
            }
            else if (movementScript.IsSliding)
            {
                targetTilt = slideTilt;
            }
            
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
            transform.localRotation = Quaternion.Euler(xRotation, 0f, currentTilt);
        }
    }
}