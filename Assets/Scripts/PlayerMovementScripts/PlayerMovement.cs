using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float wallRunSpeed = 10f; // Mirror's Edge speed boost
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Wall Running")]
    public float wallRunGravity = -1f; // Lighter gravity for "sticky" feel
    public float wallJumpUpForce = 5f;
    public float wallJumpSideForce = 4f;
    public float wallDetectionRange = 0.8f;
    public LayerMask wallLayer;

    private Vector3 playerVelocity;
    private bool isGrounded;
    
    // Properties for the Camera Script
    public bool IsWallRunning { get; private set; }
    public int WallSide { get; private set; } // -1 for Left, 1 for Right

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded)
        {
            if (playerVelocity.y < 0) playerVelocity.y = -2f;
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 10f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 10f);
        }

        HandleMovement();
        HandleWallRun();

        // Final Movement
        Vector3 gravityVector = new Vector3(0, (IsWallRunning ? wallRunGravity : gravity), 0);
        playerVelocity += gravityVector * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // --- ADD THIS LINE TO FIX ANIMATIONS ---
        Vector2 input = moveAction.ReadValue<Vector2>();
        float currentSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() ? sprintSpeed : walkSpeed);
        animator.SetFloat("Speed", input.magnitude * currentSpeed);
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        
        float targetSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() ? sprintSpeed : walkSpeed);
        controller.Move(move * targetSpeed * Time.deltaTime);

        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void HandleWallRun()
    {
        if (isGrounded)
        {
            IsWallRunning = false;
            return;
        }

        // Raycast to sides
        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallDetectionRange, wallLayer);
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallDetectionRange, wallLayer);

        if (wallLeft || wallRight)
        {
            IsWallRunning = true;
            WallSide = wallLeft ? -1 : 1;
            playerVelocity.y = Mathf.Max(playerVelocity.y, wallRunGravity); // Maintain height

            if (jumpAction.WasPressedThisFrame())
            {
                Vector3 wallNormal = wallLeft ? leftHit.normal : rightHit.normal;
                // Wall Jump: Push UP and AWAY from wall
                playerVelocity = (Vector3.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce);
                IsWallRunning = false; // Prevent immediate re-trigger
            }
        }
        else
        {
            IsWallRunning = false;
        }
    }

    void OnAnimatorIK(int layerIndex)
{
    if (animator == null) return;

    if (IsWallRunning && WallSide == 1) // Only for right wall
    {
        // 1. Set the weight (0 = normal animation, 1 = fully stuck to wall)
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

        // 2. Define where the hand should go relative to the player
        // This puts it slightly forward and to the right
        Vector3 wallHandPos = transform.position + (transform.right * 0.5f) + (transform.forward * 0.5f) + (transform.up * 1.0f);
        
        animator.SetIKPosition(AvatarIKGoal.RightHand, wallHandPos);
        // You can also set a specific rotation so the palm faces the wall
        animator.SetIKRotation(AvatarIKGoal.RightHand, transform.rotation * Quaternion.Euler(0, -90, 0));
    }
    else
    {
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
    }
}
}