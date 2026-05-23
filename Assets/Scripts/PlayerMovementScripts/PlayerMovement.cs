using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    private float fallVelocity;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float wallRunSpeed = 10f; 
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Wall Running")]
    public float wallRunGravity = -1f; 
    public float wallJumpUpForce = 5f;
    public float wallJumpSideForce = 4f;
    public float wallDetectionRange = 0.8f;
    public LayerMask wallLayer;

    [Header("Wall Climb Settings")]
    public float climbSpeed = 5f;
    public float maxClimbTime = 0.8f; 
    public float climbCheckDistance = 1f;
    private bool isClimbing = false;
    private bool hasClimbedThisJump = false; // NEW: Prevents infinite climbing

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
    private bool wasGrounded;

    [Header("Vault Settings")]
    public float vaultMaxDistance = 1.5f;
    public float vaultSpeed = 10f;
    public LayerMask vaultLayer; 
    private bool isVaulting = false;
    private Vector3 vaultTargetPosition; 
    private Vector3 vaultLedgeEdge;

    private Vector3 playerVelocity;
    private bool isGrounded;
    
    private Coroutine currentActionRoutine;

    public bool IsWallRunning { get; private set; }
    public int WallSide { get; private set; } 

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

        // Reset horizontal movement on ground
        if (isGrounded && !isSliding && !isRolling && !isVaulting && !isClimbing)
        {
            playerVelocity.x = 0f;
            playerVelocity.z = 0f;
        }

        // NEW: Reset the climb lockout when feet touch the floor (or when wall running starts)
        if (isGrounded || IsWallRunning)
        {
            hasClimbedThisJump = false;
        }

        if (!isGrounded) 
        {
            fallVelocity = playerVelocity.y;
        }

        // 1. Landing Roll
        if (!wasGrounded && isGrounded)
        {
            if (crouchAction.IsPressed() && fallVelocity < -5f)
            {
                if (isSliding || isRolling || isVaulting || isClimbing) EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformRoll());
            }
            fallVelocity = 0; 
        }

        // 2. Trigger Slide 
        if (isGrounded && crouchAction.WasPressedThisFrame() && !isVaulting && !isClimbing)
        {
            if (isSliding)
            {
                EndCurrentAction();
            }
            else if (sprintAction.IsPressed() && !isRolling)
            {
                EndCurrentAction(); 
                currentActionRoutine = StartCoroutine(PerformSlide());
            }
        }

        // 3. Normal Movement, Vault & Climb Integration
        if (!isSliding && !isRolling && !isVaulting && !isClimbing)
        {
            // Ground Vault Check
            if (jumpAction.WasPressedThisFrame() && isGrounded && CheckVault())
            {
                EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformVault());
            }
            // MODIFIED: Airborn Wall Climb Check now requires !hasClimbedThisJump
            else if (!isGrounded && jumpAction.IsPressed() && CheckWallClimb() && !hasClimbedThisJump)
            {
                EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformWallClimb());
            }
            else
            {
                HandleMovement();
                HandleWallRun();
                
                Vector3 gravityVector = new Vector3(0, (IsWallRunning ? wallRunGravity : gravity), 0);
                playerVelocity += gravityVector * Time.deltaTime;
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }

        // 4. Update Animator
        Vector2 input = moveAction.ReadValue<Vector2>();
        float currentSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() ? sprintSpeed : walkSpeed);
        animator.SetFloat("Speed", (isSliding || isRolling || isVaulting || isClimbing) ? rollSpeed : input.magnitude * currentSpeed);
        animator.SetBool("isSliding", isSliding);
        animator.SetBool("isRolling", isRolling);
        animator.SetBool("isWallRunning", IsWallRunning);
        animator.SetInteger("WallSide", WallSide);
        animator.SetBool("isVaulting", isVaulting);
        animator.SetBool("isClimbing", isClimbing);
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
        if (isGrounded) return;

        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallDetectionRange, wallLayer);
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallDetectionRange, wallLayer);

        if (wallLeft || wallRight)
        {
            IsWallRunning = true;
            WallSide = wallLeft ? -1 : 1;
            playerVelocity.y = Mathf.Max(playerVelocity.y, wallRunGravity); 

            if (jumpAction.WasPressedThisFrame())
            {
                Vector3 wallNormal = wallLeft ? leftHit.normal : rightHit.normal;
                playerVelocity = (Vector3.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce);
                IsWallRunning = false; 
            }
        }
        else
        {
            IsWallRunning = false;
        }
    }

    private bool CheckVault()
    {
        Vector3 originLow = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(originLow, transform.forward, out RaycastHit hitLow, vaultMaxDistance, vaultLayer))
        {
            Vector3 originHigh = transform.position + (Vector3.up * originalHeight);
            if (!Physics.Raycast(originHigh, transform.forward, vaultMaxDistance, vaultLayer))
            {
                Vector3 originDown = hitLow.point + (transform.forward * 0.5f) + (Vector3.up * originalHeight);
                if (Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, originalHeight * 1.5f, vaultLayer))
                {
                    // Inside CheckVault(), after finding hitDown
                    vaultTargetPosition = hitDown.point + (Vector3.up * (originalHeight / 2f + 0.1f)); 
                    // The edge is the XZ of the wall and the Y of the top surface
                    vaultLedgeEdge = new Vector3(hitLow.point.x, hitDown.point.y, hitLow.point.z); 
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckWallClimb()
    {
        Vector3 chest = transform.position + (Vector3.up * (originalHeight / 2f));
        return Physics.Raycast(chest, transform.forward, climbCheckDistance, vaultLayer);
    }

    private bool CheckLedgeDuringClimb()
    {
        Vector3 originHigh = transform.position + (Vector3.up * originalHeight);
        
        if (!Physics.Raycast(originHigh, transform.forward, climbCheckDistance, vaultLayer))
        {
            Vector3 originDown = originHigh + (transform.forward * 0.8f);
            if (Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, originalHeight, vaultLayer))
            {
                vaultTargetPosition = hitDown.point + (Vector3.up * (originalHeight / 2f + 0.1f)); 
                return true;
            }
        }
        return false;
    }

    private System.Collections.IEnumerator PerformWallClimb()
    {
        isClimbing = true;
        hasClimbedThisJump = true; // NEW: Lock out further climbs until grounded
        
        float timer = 0f;
        playerVelocity = Vector3.zero;

        while (jumpAction.IsPressed() && timer < maxClimbTime)
        {
            if (!CheckWallClimb()) break;

            Vector3 climbMove = (Vector3.up * climbSpeed) + (transform.forward * 2f);
            controller.Move(climbMove * Time.deltaTime);

            if (CheckLedgeDuringClimb())
            {
                EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformVault());
                yield break; 
            }

            timer += Time.deltaTime;
            yield return null;
        }

        EndCurrentAction();
    }

    private System.Collections.IEnumerator PerformVault()
    {
        isVaulting = true;
        animator.SetBool("isVaulting", true);
        playerVelocity = Vector3.zero; 

        Vector3 startPos = transform.position;
        float percent = 0f;
        
        float distance = Vector3.Distance(startPos, vaultTargetPosition);
        float duration = 0.6f; // Adjust this to match your Vault.anim length

        while (percent < 1f)
        {
            percent += Time.deltaTime / duration;
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percent); 
            Vector3 targetPosThisFrame = Vector3.Lerp(startPos, vaultTargetPosition, smoothPercent);
            
            // Optional: Use MatchTarget if your animation is set up for it.
            // This helps the hand bone reach the vaultLedgeEdge at a specific time (e.g., 30% into the anim).
            if (!animator.IsInTransition(0) && animator.GetCurrentAnimatorStateInfo(0).IsName("Vault"))
            {
                animator.MatchTarget(vaultLedgeEdge, Quaternion.identity, AvatarTarget.LeftHand, 
                    new MatchTargetWeightMask(Vector3.one, 0), 0.2f, 0.4f);
            }

            controller.Move(targetPosThisFrame - transform.position);
            yield return null;
        }

        isVaulting = false;
        animator.SetBool("isVaulting", false);
        EndCurrentAction();
    }

    private void EndCurrentAction()
    {
        if (currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }

        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
        isRolling = false;
        isVaulting = false; 
        isClimbing = false; 
        
        playerVelocity.x = 0;
        playerVelocity.z = 0; 
    }

    private System.Collections.IEnumerator PerformSlide()
    {
        isSliding = true;
        playerVelocity = Vector3.zero;

        controller.height = slideHeight;
        controller.center = new Vector3(0, originalCenter.y - (originalHeight / 2f) + (slideHeight / 2f), 0);

        float timer = 0f;
        Vector3 slideDirection = transform.forward;

        while (timer < slideDuration)
        {
            Vector3 currentMove = slideDirection * slideSpeed;
            
            if (!controller.isGrounded) {
                playerVelocity.y += gravity * Time.deltaTime;
            } else {
                playerVelocity.y = -2f; 
            }
            
            currentMove.y = playerVelocity.y;
            controller.Move(currentMove * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        EndCurrentAction();
    }

    private System.Collections.IEnumerator PerformRoll()
    {
        isRolling = true;
        playerVelocity = Vector3.zero;

        controller.height = slideHeight; 
        controller.center = new Vector3(0, originalCenter.y - (originalHeight / 2f) + (slideHeight / 2f), 0);

        float timer = 0f;
        Vector3 rollDirection = transform.forward;

        while (timer < rollDuration)
        {
            Vector3 currentMove = rollDirection * rollSpeed;
            
            if (!controller.isGrounded) {
                playerVelocity.y += gravity * Time.deltaTime;
            } else {
                playerVelocity.y = -2f; 
            }
            
            currentMove.y = playerVelocity.y;
            controller.Move(currentMove * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null;
        }

        EndCurrentAction();
    }
}