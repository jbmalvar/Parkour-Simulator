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

    // NEW: Brute-Force Time Stop Logic
    private IEnumerator PerformTimeStop()
    {
        Debug.Log("ZA WARUDO! Time Stopped.");
        
        float timer = 0f;
        float defaultFixedDelta = 0.02f; // Unity's default physics step
        
        // Loop every single frame until the 5 seconds are up
        while (timer < timeStopDuration)
        {
            // OVERRIDE: Force time to be slow every single frame.
            // This completely bullies any other script trying to set time back to 1.
            Time.timeScale = timeScaleDuringStop;
            Time.fixedDeltaTime = defaultFixedDelta * Time.timeScale;
            
            // Count up using real-world time
            timer += Time.unscaledDeltaTime; 
            
            // Wait until the next frame and enforce it again
            yield return null; 
        }

        // Once the loop finishes, put the game back to normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        
        Debug.Log("Time resumed.");
    }
}