using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAbilities : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private CharacterController controller;
    private PlayerMana playerMana; 
    private PlayerStamina playerStamina;
    private PlayerHealth playerHealth; // ---> NEW: Reference to health <---

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

    [Header("Infinite Stamina Settings")]
    [Tooltip("How many seconds of infinite stamina you get per 1 point of mana consumed.")]
    public float staminaDurationPerMana = 0.1f; 
    private Coroutine infiniteStaminaRoutine;

    // ---> NEW: Quick Regen Settings <---
    [Header("Quick Regen Settings")]
    public float regenManaCost = 40f;
    public int regenTotalAmount = 50; // Total health to restore
    public float regenDuration = 1.5f; // How fast to restore it
    private Coroutine regenRoutine;

    private Coroutine currentAbilityRoutine;
    private Coroutine timeStopRoutine; 

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        playerMana = GetComponent<PlayerMana>(); 
        playerStamina = GetComponent<PlayerStamina>();
        playerHealth = GetComponent<PlayerHealth>(); // ---> NEW: Grab health component <---
    }

    public void TriggerGenjiDash()
    {
        if (playerMana != null && !playerMana.TryUseMana(dashManaCost)) return; 

        playerMovement.EndCurrentAction(); 
        if (currentAbilityRoutine != null) StopCoroutine(currentAbilityRoutine);
        currentAbilityRoutine = StartCoroutine(PerformGenjiDash());
    }

    public void TriggerSuperJump()
    {
        if (playerMana != null && !playerMana.TryUseMana(superJumpManaCost)) return;

        playerMovement.EndCurrentAction();
        float baseJumpHeight = playerMovement.jumpHeight;
        float gravity = playerMovement.gravity;

        playerMovement.playerVelocity.y = Mathf.Sqrt((baseJumpHeight * superJumpMultiplier) * -2f * gravity); 
    }

    public void TriggerTimeStop()
    {
        if (playerMana != null && !playerMana.TryUseMana(timeStopManaCost)) return;

        if (timeStopRoutine != null) StopCoroutine(timeStopRoutine);
        timeStopRoutine = StartCoroutine(PerformTimeStop());
    }

    public void TriggerInfiniteStamina()
    {
        if (playerMana == null) return;

        float manaConsumed = playerMana.DrainAllMana();

        if (manaConsumed <= 0f) 
        {
            Debug.Log("No mana to convert to stamina!");
            return;
        }

        if (infiniteStaminaRoutine != null) StopCoroutine(infiniteStaminaRoutine);
        infiniteStaminaRoutine = StartCoroutine(PerformInfiniteStamina(manaConsumed));
    }

    // ---> NEW: Trigger Quick Regen <---
    public void TriggerQuickRegen()
    {
        if (playerMana != null && !playerMana.TryUseMana(regenManaCost)) return;
        if (playerHealth == null) return;

        if (regenRoutine != null) StopCoroutine(regenRoutine);
        regenRoutine = StartCoroutine(PerformQuickRegen());
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

    private IEnumerator PerformInfiniteStamina(float manaConsumed)
    {
        float calculatedDuration = manaConsumed * staminaDurationPerMana;
        Debug.Log($"Stamina Overload! Infinite stamina for {calculatedDuration} seconds.");

        float timer = 0f;
        while (timer < calculatedDuration)
        {
            if (playerStamina != null)
            {
                playerStamina.SetInfiniteStamina(true);
            }

            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        if (playerStamina != null)
        {
            playerStamina.SetInfiniteStamina(false);
        }

        Debug.Log("Infinite stamina ended.");
    }

    // ---> NEW: Perform Quick Regen <---
    private IEnumerator PerformQuickRegen()
    {
        Debug.Log("Quick Regen Started!");
        
        // We break the total heal amount into 10 rapid "ticks"
        int ticks = 10;
        int healPerTick = regenTotalAmount / ticks;
        float timeBetweenTicks = regenDuration / ticks;

        for (int i = 0; i < ticks; i++)
        {
            playerHealth.Heal(healPerTick);
            // Using Realtime so you can still heal while time is stopped!
            yield return new WaitForSecondsRealtime(timeBetweenTicks); 
        }

        Debug.Log("Quick Regen Finished.");
    }
}