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
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Wall Running")]
    public float wallRunGravity = -2f;
    public float wallRunJumpForce = 5f;
    public float wallDetectionRange = 0.7f;
    public LayerMask wallLayer;

    private Vector3 playerVelocity; // For gravity and jumps
    private bool isGrounded;
    private bool isWallRunning;

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
        if (isGrounded && playerVelocity.y < 0) 
            playerVelocity.y = -2f;

        // 1. Get Input and calculate Horizontal Move
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = transform.right * input.x + transform.forward * input.y;
        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        
        // 2. Handle Jumping and Gravity
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        HandleWallRun();

        float currentGravity = isWallRunning ? wallRunGravity : gravity;
        playerVelocity.y += currentGravity * Time.deltaTime;

        // 3. COMBINE and Move ONCE (Critical for velocity detection)
        Vector3 finalMovement = (moveDirection * currentSpeed) + playerVelocity;
        controller.Move(finalMovement * Time.deltaTime);

        // 4. Update Animator based on actual displacement
        // We use the input magnitude here to ensure animations play even if stuck against a wall
        float moveMagnitude = input.magnitude * currentSpeed;
        animator.SetFloat("Speed", moveMagnitude);
    }

    void HandleWallRun()
    {
        if (isGrounded)
        {
            isWallRunning = false;
            return;
        }

        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallDetectionRange, wallLayer);
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallDetectionRange, wallLayer);

        if (wallLeft || wallRight)
        {
            isWallRunning = true;
            playerVelocity.y = Mathf.Max(playerVelocity.y, wallRunGravity);

            if (jumpAction.WasPressedThisFrame())
            {
                Vector3 wallNormal = wallLeft ? leftHit.normal : rightHit.normal;
                // Add wall kick force to playerVelocity
                playerVelocity = (wallNormal + Vector3.up).normalized * wallRunJumpForce;
            }
        }
        else
        {
            isWallRunning = false;
        }
    }
}