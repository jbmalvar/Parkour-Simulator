using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Wall Running")]
    public float wallRunGravity = -2f;
    public float wallRunJumpForce = 5f;
    public float wallDetectionRange = 0.7f;
    public LayerMask wallLayer;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isWallRunning;

    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Hook into Project-Wide Input Actions
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    void Update()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) velocity.y = -2f;

        HandleMovement();
        HandleWallRun();
        
        // Final Movement
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        
        float currentSpeed = sprintAction.IsPressed() ? sprintSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Standard Jump
        if (jumpAction.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply Gravity
        float currentGravity = isWallRunning ? wallRunGravity : gravity;
        velocity.y += currentGravity * Time.deltaTime;
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
            velocity.y = Mathf.Max(velocity.y, wallRunGravity); // Maintain height longer

            // Jump off wall
            if (jumpAction.WasPressedThisFrame())
            {
                Vector3 wallNormal = wallLeft ? leftHit.normal : rightHit.normal;
                velocity = (wallNormal + Vector3.up).normalized * wallRunJumpForce;
            }
        }
        else
        {
            isWallRunning = false;
        }
    }
}