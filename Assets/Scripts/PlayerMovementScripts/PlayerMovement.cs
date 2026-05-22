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

    
    [Header("Slide Settings")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.8f;
    public float slideHeight = 1f; 
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;

    private InputAction crouchAction;

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
        crouchAction = InputSystem.actions.FindAction("Crouch");

        originalHeight = controller.height;
        originalCenter = controller.center;
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

        // TRIGGER SLIDE: Grounded + Sprinting + Crouch button pressed
        if (isGrounded && sprintAction.IsPressed() && crouchAction.WasPressedThisFrame() && !isSliding)
        {
            StartCoroutine(PerformSlide());
        }

        // Only handle normal movement and wall running if NOT sliding
        if (!isSliding)
        {
            HandleMovement();
            HandleWallRun();
        }
        else
        {
            // Allow jumping out of a slide (Slide Jump)
            if (jumpAction.WasPressedThisFrame() && isGrounded)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                // Sliding is automatically turned off when the coroutine ends or by jumping
            }
        }

        // Final Movement (Applies Gravity and Jump forces)
        Vector3 gravityVector = new Vector3(0, (IsWallRunning ? wallRunGravity : gravity), 0);
        playerVelocity += gravityVector * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        // Update Animation Parameters
        Vector2 input = moveAction.ReadValue<Vector2>();
        float currentSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() ? sprintSpeed : walkSpeed);
        
        // If sliding, tell the animator to use slide speed, otherwise use normal speed
        animator.SetFloat("Speed", isSliding ? slideSpeed : input.magnitude * currentSpeed);
        animator.SetBool("isSliding", isSliding);
        animator.SetBool("isWallRunning", IsWallRunning);
        animator.SetInteger("WallSide", WallSide);
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

    private System.Collections.IEnumerator PerformSlide()
    {
        isSliding = true;
        
        // Shrink the controller to slide under obstacles
        controller.height = slideHeight;
        controller.center = new Vector3(0, slideHeight / 2f, 0);

        float timer = 0f;
        Vector3 slideDirection = transform.forward; // Slide in the direction the player is facing

        while (timer < slideDuration)
        {
            // Move the player forward at slide speed
            controller.Move(slideDirection * slideSpeed * Time.deltaTime);
            
            // Optional: Apply some gravity while sliding if not grounded
            if (!controller.isGrounded)
            {
                controller.Move(Vector3.up * gravity * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Reset controller dimensions
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
    }
}