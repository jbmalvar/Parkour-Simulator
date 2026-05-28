using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAbilities : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private CharacterController controller;

    [Header("Genji Dash Settings")]
    public float dashSpeed = 35f;
    public float dashDuration = 0.25f;

    [Header("Super Jump Settings")]
    public float superJumpMultiplier = 3f;

    [Header("Time Stop Settings")]
    public float timeStopDuration = 5f;
    [Tooltip("How slow time goes. 0.05f is basically frozen but looks cooler than a hard 0.")]
    public float timeScaleDuringStop = 0.05f; 

    private Coroutine currentAbilityRoutine;
    private Coroutine timeStopRoutine; // Track separately so dashing doesn't cancel time stop

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
    }

    public void TriggerGenjiDash()
    {
        playerMovement.EndCurrentAction(); 
        if (currentAbilityRoutine != null) StopCoroutine(currentAbilityRoutine);
        currentAbilityRoutine = StartCoroutine(PerformGenjiDash());
    }

    public void TriggerSuperJump()
    {
        playerMovement.EndCurrentAction();
        float baseJumpHeight = playerMovement.jumpHeight;
        float gravity = playerMovement.gravity;

        playerMovement.playerVelocity.y = Mathf.Sqrt((baseJumpHeight * superJumpMultiplier) * -2f * gravity); 
    }

    // NEW: Time Stop Trigger
    public void TriggerTimeStop()
    {
        if (timeStopRoutine != null) StopCoroutine(timeStopRoutine);
        timeStopRoutine = StartCoroutine(PerformTimeStop());
    }

    private IEnumerator PerformGenjiDash()
    {
        float timer = 0f;
        playerMovement.playerVelocity = Vector3.zero;
        Vector3 dashDirection = Camera.main.transform.forward;

        while (timer < dashDuration)
        {
            // Note: dash relies on unscaledDeltaTime, which is great for time stops!
            controller.Move(dashDirection * dashSpeed * Time.unscaledDeltaTime);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        playerMovement.playerVelocity = dashDirection * playerMovement.walkSpeed; 
    }

    // NEW: Time Stop Logic
    private IEnumerator PerformTimeStop()
    {
        Debug.Log("ZA WARUDO! Time Stopped.");
        
        // Store original fixed delta time (important for physics stability)
        float originalFixedDelta = Time.fixedDeltaTime;

        // Slow down time
        Time.timeScale = timeScaleDuringStop;
        Time.fixedDeltaTime = originalFixedDelta * Time.timeScale; // Keep physics calculations in sync

        // Wait for the duration in REAL time, not game time
        yield return new WaitForSecondsRealtime(timeStopDuration);

        // Restore time
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDelta;
        
        Debug.Log("Time resumed.");
    }
}