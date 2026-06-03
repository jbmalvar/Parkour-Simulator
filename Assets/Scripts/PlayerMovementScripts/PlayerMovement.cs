using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private Animator animator;
    private CharacterController controller;
    private float fallVelocity;

    [Header("Fall Damage Settings")]
    public float fatalFallSpeed = -18f; // Speed at which player dies without rolling
    public float maxRollSurvivalSpeed = -25f; // Speed at which player dies EVEN IF they roll

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 1.5f;
    public float gravity = -19.62f;

    [Header("Crouch Settings")]
    public float crouchSpeed = 2.5f;
    public float crouchHeight = 1f;
    public bool IsCrouching { get; private set; }

    [Header("Wall Running (Mirror's Edge Style)")]
    public float wallRunSpeed = 10f; 
    public float wallRunGravity = -1.5f; // Gentle downward slide instead of falling
    public float wallJumpUpForce = 6f;
    public float wallJumpSideForce = 8f; // Strong outward push
    public float wallRunMaxDuration = 1.5f; // How long you can run before dropping
    public float wallDetectionRange = 0.8f;
    public LayerMask wallLayer;
    private float wallRunTimer;

    [Header("Wall Climb Settings")]
    public float climbSpeed = 5f;
    public float maxClimbTime = 0.8f; 
    public float climbCheckDistance = 1f;
    private bool isClimbing = false;
    private bool hasClimbedThisJump = false; 

    [Header("Slide Settings")]
    public float slideSpeed = 12f;
    public float slideDuration = 0.8f;
    public float slideHeight = 1f; 
    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    public bool IsSliding => isSliding;
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

    public Vector3 playerVelocity;
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

        // NEW: Reset gravity when grounded so it doesn't endlessly accumulate
        if (isGrounded && playerVelocity.y < 0)
        {
            // We use -2f instead of 0f to push the player slightly into the floor.
            // If it's exactly 0, the CharacterController sometimes thinks you are floating 
            // and isGrounded will rapidly flicker between true and false.
            playerVelocity.y = -2f; 
        }

        if (isGrounded && !isSliding && !isRolling && !isVaulting && !isClimbing)
        {
            playerVelocity.x = 0f;
            playerVelocity.z = 0f;
        }

        if (isGrounded && !isSliding && !isRolling && !isVaulting && !isClimbing)
        {
            playerVelocity.x = 0f;
            playerVelocity.z = 0f;
        }

        if (isGrounded || IsWallRunning)
        {
            hasClimbedThisJump = false;
        }

        if (!isGrounded) 
        {
            fallVelocity = playerVelocity.y;
        }

        // // 1. Landing Roll
        // if (!wasGrounded && isGrounded)
        // {
        //     if (crouchAction.IsPressed() && fallVelocity < -5f)
        //     {
        //         if (isSliding || isRolling || isVaulting || isClimbing) EndCurrentAction();
        //         currentActionRoutine = StartCoroutine(PerformRoll());
        //     }
        //     fallVelocity = 0; 
        // }
        // 1. Landing Roll & Fall Damage
        if (!wasGrounded && isGrounded)
        {
            // Check if the player is pressing crouch fast enough to trigger a roll
            bool tryingToRoll = crouchAction.IsPressed() && fallVelocity < -5f;

            if (tryingToRoll && fallVelocity >= maxRollSurvivalSpeed)
            {
                // Player successfully rolled AND didn't exceed the absolute max survival speed
                if (isSliding || isRolling || isVaulting || isClimbing) EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformRoll());
            }
            else if (fallVelocity <= fatalFallSpeed)
            {
                // Player either didn't roll, or fell so fast that even a roll couldn't save them
                CheckpointManager.Instance?.TriggerDeath();
                
                // Kill momentum so the corpse/camera doesn't keep sliding
                playerVelocity = Vector3.zero; 
            }

            fallVelocity = 0; 
        }

        // 2. Trigger Slide / Crouch 
        if (isGrounded && !isVaulting && !isClimbing && !isRolling)
        {
            if (crouchAction.WasPressedThisFrame() && sprintAction.IsPressed() && !isSliding)
            {
                EndCurrentAction(); 
                currentActionRoutine = StartCoroutine(PerformSlide());
            }
            else if (crouchAction.IsPressed() && !isSliding)
            {
                IsCrouching = true;
                controller.height = crouchHeight;
                controller.center = new Vector3(0, originalCenter.y - (originalHeight - crouchHeight) / 2f, 0);
            }
            else if (!crouchAction.IsPressed() && !isSliding)
            {
                if (IsCrouching && CanStand())
                {
                    IsCrouching = false;
                    controller.height = originalHeight;
                    controller.center = originalCenter;
                }
            }
        }

        // 3. Normal Movement, Vault & Climb Integration
        if (!isSliding && !isRolling && !isVaulting && !isClimbing)
        {
            if (jumpAction.WasPressedThisFrame() && isGrounded && (!IsCrouching || CanStand()))
            {
                if (CheckVault())
                {
                    EndCurrentAction();
                    currentActionRoutine = StartCoroutine(PerformVault());
                }
                else
                {
                    playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }
            else if (!isGrounded && jumpAction.IsPressed() && CheckWallClimb() && !hasClimbedThisJump)
            {
                EndCurrentAction();
                currentActionRoutine = StartCoroutine(PerformWallClimb());
            }
            else
            {
                // Check Wall Run logic first
                HandleWallRun();
                
                // Only allow normal horizontal movement and standard gravity if NOT wall running
                if (!IsWallRunning)
                {
                    HandleMovement();
                    playerVelocity.y += gravity * Time.deltaTime;
                }
                
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }

        // 4. Update Animator
        Vector2 input = moveAction.ReadValue<Vector2>();
        float currentSpeed = IsWallRunning ? wallRunSpeed : (sprintAction.IsPressed() && !IsCrouching ? sprintSpeed : walkSpeed);
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
        
        float targetSpeed = walkSpeed;
        if (IsCrouching) targetSpeed = crouchSpeed;
        else if (sprintAction.IsPressed()) targetSpeed = sprintSpeed;

        controller.Move(move * targetSpeed * Time.deltaTime);
    }

    private bool CanStand()
    {
        Vector3 rayStart = transform.position + Vector3.up * controller.height;
        float distance = (originalHeight - controller.height) + 0.1f;
        return !Physics.SphereCast(rayStart, controller.radius, Vector3.up, out _, distance);
    }

    void HandleWallRun()
    {
        if (isGrounded) 
        {
            IsWallRunning = false;
            return;
        }

        Vector2 input = moveAction.ReadValue<Vector2>();
        
        // You MUST be pressing forward to wall run (Mirror's edge momentum logic)
        if (input.y <= 0)
        {
            IsWallRunning = false;
            return;
        }

        bool wallLeft = Physics.Raycast(transform.position, -transform.right, out RaycastHit leftHit, wallDetectionRange, wallLayer);
        bool wallRight = Physics.Raycast(transform.position, transform.right, out RaycastHit rightHit, wallDetectionRange, wallLayer);

        if (wallLeft || wallRight)
        {
            RaycastHit wallHit = wallLeft ? leftHit : rightHit;
            Vector3 wallNormal = wallHit.normal;

            // Prevent Wall Running if facing directly head-on into the wall (Leave that for climbing)
            if (Vector3.Dot(transform.forward, wallNormal) < -0.75f)
            {
                IsWallRunning = false;
                return;
            }

            if (!IsWallRunning)
            {
                // Enter Wall Run State
                IsWallRunning = true;
                WallSide = wallLeft ? -1 : 1;
                
                // KILL UPWARD MOMENTUM: Stops you from floating higher when hitting the wall
                playerVelocity = Vector3.zero; 
                wallRunTimer = 0f;
            }

            wallRunTimer += Time.deltaTime;

            if (wallRunTimer < wallRunMaxDuration)
            {
                // 1. Calculate trajectory perfectly parallel to the wall
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
                
                // Ensure we are going the way the player is facing
                if (Vector3.Dot(transform.forward, wallForward) < 0)
                {
                    wallForward = -wallForward;
                }

                // 2. Stick to wall (slight inward push so raycast doesn't lose the wall)
                Vector3 stickToWall = -wallNormal * 2f; 
                
                // 3. Move along the wall
                Vector3 moveDirection = (wallForward * wallRunSpeed) + stickToWall;
                controller.Move(moveDirection * Time.deltaTime);

                // 4. Apply custom gentle gravity (instead of falling like a rock)
                playerVelocity.y = wallRunGravity;

                // 5. Wall Jump Exit
                if (jumpAction.WasPressedThisFrame())
                {
                    // Propel the player Up, Out from the wall, and slightly Forward
                    playerVelocity = (Vector3.up * wallJumpUpForce) + (wallNormal * wallJumpSideForce) + (transform.forward * (walkSpeed));
                    IsWallRunning = false;
                }
            }
            else
            {
                // Timer ran out, drop off the wall
                IsWallRunning = false;
            }
        }
        else
        {
            IsWallRunning = false;
        }
    }

    private bool CheckVault() { 
        Vector3 originLow = transform.position + (Vector3.up * 0.5f);
        if (Physics.Raycast(originLow, transform.forward, out RaycastHit hitLow, vaultMaxDistance, vaultLayer))
        {
            Vector3 originHigh = transform.position + (Vector3.up * originalHeight);
            if (!Physics.Raycast(originHigh, transform.forward, vaultMaxDistance, vaultLayer))
            {
                Vector3 originDown = hitLow.point + (transform.forward * 0.5f) + (Vector3.up * originalHeight);
                if (Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, originalHeight * 1.5f, vaultLayer))
                {
                    vaultTargetPosition = hitDown.point + (Vector3.up * (originalHeight / 2f + 0.1f)); 
                    vaultLedgeEdge = new Vector3(hitLow.point.x, hitDown.point.y, hitLow.point.z); 
                    return true;
                }
            }
        }
        return false;
    }

    private bool CheckWallClimb() { 
        Vector3 chest = transform.position + (Vector3.up * (originalHeight / 2f));
        return Physics.Raycast(chest, transform.forward, climbCheckDistance, vaultLayer);
    }

    private bool CheckLedgeDuringClimb() { 
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

    private System.Collections.IEnumerator PerformWallClimb() { 
        isClimbing = true;
        hasClimbedThisJump = true; 
        
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

    private System.Collections.IEnumerator PerformVault() { 
        isVaulting = true;
        animator.SetBool("isVaulting", true);
        playerVelocity = Vector3.zero; 

        Vector3 startPos = transform.position;
        float percent = 0f;
        
        float distance = Vector3.Distance(startPos, vaultTargetPosition);
        float duration = 0.6f; 

        while (percent < 1f)
        {
            percent += Time.deltaTime / duration;
            float smoothPercent = Mathf.SmoothStep(0f, 1f, percent); 
            Vector3 targetPosThisFrame = Vector3.Lerp(startPos, vaultTargetPosition, smoothPercent);
            
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

    public void EndCurrentAction()
    {
        if (currentActionRoutine != null)
        {
            StopCoroutine(currentActionRoutine);
            currentActionRoutine = null;
        }

        isSliding = false;
        isRolling = false;
        isVaulting = false; 
        isClimbing = false; 
        
        if (!CanStand() || crouchAction.IsPressed())
        {
            IsCrouching = true;
            controller.height = crouchHeight;
            controller.center = new Vector3(0, originalCenter.y - (originalHeight - crouchHeight) / 2f, 0);
        }
        else
        {
            IsCrouching = false;
            controller.height = originalHeight;
            controller.center = originalCenter;
        }
        
        playerVelocity.x = 0;
        playerVelocity.z = 0; 
    }

    private System.Collections.IEnumerator PerformSlide() { 
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

    private System.Collections.IEnumerator PerformRoll() { 
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