using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAbilities : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private CharacterController controller;
    
    // ---> NEW: Reference to the mana bank <---
    private PlayerMana playerMana; 

    [Header("Mana Costs")]
    public float dashManaCost = 20f;
    public float superJumpManaCost = 30f;
    public float timeStopManaCost = 50f;

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
    private Coroutine timeStopRoutine; 

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        
        // ---> NEW: Grab the mana script <---
        playerMana = GetComponent<PlayerMana>(); 
    }

    public void TriggerGenjiDash()
    {
        // ---> NEW: Check mana before casting <---
        if (playerMana != null && !playerMana.TryUseMana(dashManaCost)) return; 

        playerMovement.EndCurrentAction(); 
        if (currentAbilityRoutine != null) StopCoroutine(currentAbilityRoutine);
        currentAbilityRoutine = StartCoroutine(PerformGenjiDash());
    }

    public void TriggerSuperJump()
    {
        // ---> NEW: Check mana before casting <---
        if (playerMana != null && !playerMana.TryUseMana(superJumpManaCost)) return;

        playerMovement.EndCurrentAction();
        float baseJumpHeight = playerMovement.jumpHeight;
        float gravity = playerMovement.gravity;

        playerMovement.playerVelocity.y = Mathf.Sqrt((baseJumpHeight * superJumpMultiplier) * -2f * gravity); 
    }

    public void TriggerTimeStop()
    {
        // ---> NEW: Check mana before casting <---
        if (playerMana != null && !playerMana.TryUseMana(timeStopManaCost)) return;

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
            controller.Move(dashDirection * dashSpeed * Time.unscaledDeltaTime);
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        playerMovement.playerVelocity = dashDirection * playerMovement.walkSpeed; 
    }

    private IEnumerator PerformTimeStop()
    {
        Debug.Log("ZA WARUDO! Time Stopped.");
        
        float timer = 0f;
        float defaultFixedDelta = 0.02f; 
        
        while (timer < timeStopDuration)
        {
            Time.timeScale = timeScaleDuringStop;
            Time.fixedDeltaTime = defaultFixedDelta * Time.timeScale;
            
            timer += Time.unscaledDeltaTime; 
            yield return null; 
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDelta;
        
        Debug.Log("Time resumed.");
    }
}