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

    [Header("Roll Settings")]
    public float rollSpeed = 10f;
    public float rollDuration = 1.0f;
    private bool isRolling = false;
    public bool IsRolling => isRolling;
    private bool wasGrounded; // To detect the moment of landing

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
        wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;

        // Detect Landing Roll: Now checks !isSliding to prevent stacking
        if (!wasGrounded && isGrounded && crouchAction.IsPressed() && !isRolling && !isSliding)
        {
            StartCoroutine(PerformRoll());
        }

        if (isGrounded)
        {
            if (playerVelocity.y < 0) playerVelocity.y = -2f;
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 10f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 10f);
        }

        // Trigger Slide: Added safety
        if (isGrounded && sprintAction.IsPressed() && crouchAction.WasPressedThisFrame() && !isSliding && !isRolling)
        {
            StartCoroutine(PerformSlide());
        }

        // Only move normally if NOT sliding or rolling
        if (!isSliding && !isRolling)
        {
            HandleMovement();
            HandleWallRun();
            
            // Apply Gravity only during normal movement (Coroutines handle their own gravity)
            Vector3 gravityVector = new Vector3(0, (IsWallRunning ? wallRunGravity : gravity), 0);
            playerVelocity += gravityVector * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);
        }

        // Update Animator
        Vector2 input = moveAction.ReadValue<Vector2>();
        float currentSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() ? sprintSpeed : walkSpeed);
        animator.SetFloat("Speed", (isSliding || isRolling) ? rollSpeed : input.magnitude * currentSpeed);
        animator.SetBool("isSliding", isSliding);
        animator.SetBool("isRolling", isRolling);
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

    // 2. Refined Slide Coroutine
    private System.Collections.IEnumerator PerformSlide()
    {
        // If we were already doing something, stop it
        isRolling = false; 
        isSliding = true;
        playerVelocity = Vector3.zero;

        controller.height = slideHeight;
        controller.center = new Vector3(0, originalCenter.y - (originalHeight / 2f) + (slideHeight / 2f), 0);

        float timer = 0f;
        Vector3 slideDirection = transform.forward;

        while (timer < slideDuration)
        {
            // Move horizontally
            controller.Move(slideDirection * slideSpeed * Time.deltaTime);
            
            // Apply a small amount of gravity manually so we don't float off ledges
            if (!controller.isGrounded)
            {
                playerVelocity.y += gravity * Time.deltaTime;
                controller.Move(new Vector3(0, playerVelocity.y, 0) * Time.deltaTime);
            }
            else
            {
                playerVelocity.y = -2f; // Keep stuck to ground
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Reset
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
        playerVelocity = Vector3.zero; // Clear any built-up gravity
    }

    // 3. Refined Roll Coroutine
    private System.Collections.IEnumerator PerformRoll()
    {
        isSliding = false;
        isRolling = true;
        playerVelocity = Vector3.zero;

        float timer = 0f;
        Vector3 rollDirection = transform.forward;

        while (timer < rollDuration)
        {
            controller.Move(rollDirection * rollSpeed * Time.deltaTime);
            
            // Constant ground check during roll
            if (!controller.isGrounded)
            {
                playerVelocity.y += gravity * Time.deltaTime;
                controller.Move(new Vector3(0, playerVelocity.y, 0) * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        isRolling = false;
        playerVelocity = Vector3.zero;
    }
}