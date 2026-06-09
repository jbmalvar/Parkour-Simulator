using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerMovement), typeof(CharacterController))]
public class PlayerAbilities : MonoBehaviour
{
    private PlayerMovement playerMovement;
    private CharacterController controller;
    private PlayerMana playerMana; 
    private PlayerStamina playerStamina;
    private PlayerHealth playerHealth;

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
    public float timeStopDuration = 7f;
    [Tooltip("How slow time goes. 0.05f is basically frozen but looks cooler than a hard 0.")]
    public float timeScaleDuringStop = 0.05f; 

    [Header("Infinite Stamina Settings")]
    [Tooltip("How many seconds of infinite stamina you get per 1 point of mana consumed.")]
    public float staminaDurationPerMana = 0.1f; 
    private Coroutine infiniteStaminaRoutine;

    [Header("Quick Regen Settings")]
    public float regenManaCost = 40f;
    public int regenTotalAmount = 50; 
    public float regenDuration = 1.5f; 
    private Coroutine regenRoutine;

    [Header("Mana Burst Settings")]
    public float manaBurstTotalAmount = 50f; 
    public float manaBurstDuration = 1.0f;  
    private Coroutine manaBurstRoutine;

    [Header("Safe Fall Settings")]
    public float safeFallManaCost = 20f;
    public float safeFallDuration = 8f; 
    private Coroutine safeFallRoutine;

    [Header("Speed Boost Settings")]
    public float speedBoostManaCost = 30f;
    public float speedBoostMultiplier = 2f; 
    public float speedBoostDuration = 6f;
    private Coroutine speedBoostRoutine;

    [Header("Night Light Settings")]
    public float lightManaCost = 15f;
    public float lightDuration = 10f; 
    [Tooltip("Color of the magic light or night vision.")]
    public Color lightColor = new Color(0.8f, 1f, 0.8f); 
    public float lightIntensity = 5f;
    public float lightRange = 20f;
    private Coroutine lightRoutine;
    private GameObject activeLight;
    private Coroutine currentAbilityRoutine;
    private Coroutine timeStopRoutine; 

    public bool isTimeStopActive = false; 

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        playerMana = GetComponent<PlayerMana>(); 
        playerStamina = GetComponent<PlayerStamina>();
        playerHealth = GetComponent<PlayerHealth>(); 
    }

    public bool TriggerGenjiDash()
    {
        // Add "false" here
        if (playerMana != null && !playerMana.TryUseMana(dashManaCost)) return false; 

        playerMovement.EndCurrentAction(); 
        if (currentAbilityRoutine != null) StopCoroutine(currentAbilityRoutine);
        currentAbilityRoutine = StartCoroutine(PerformGenjiDash());
        
        return true; // Add "true" at the end!
    }

    public bool TriggerSuperJump()
    {
        if (playerMana != null && !playerMana.TryUseMana(superJumpManaCost)) return false;

        playerMovement.EndCurrentAction();
        float baseJumpHeight = playerMovement.jumpHeight;
        float gravity = playerMovement.gravity;

        playerMovement.playerVelocity.y = Mathf.Sqrt((baseJumpHeight * superJumpMultiplier) * -2f * gravity); 
        
        return true;
    }

    public bool TriggerTimeStop()
    {
        if (playerMana != null && !playerMana.TryUseMana(timeStopManaCost)) return false;

        if (timeStopRoutine != null) StopCoroutine(timeStopRoutine);
        timeStopRoutine = StartCoroutine(PerformTimeStop());
        
        return true;
    }

    public bool TriggerInfiniteStamina()
    {
        if (playerMana == null) return false;

        float manaConsumed = playerMana.DrainAllMana();

        if (manaConsumed <= 0f) 
        {
            Debug.Log("No mana to convert to stamina!");
            return false;
        }

        if (infiniteStaminaRoutine != null) StopCoroutine(infiniteStaminaRoutine);
        infiniteStaminaRoutine = StartCoroutine(PerformInfiniteStamina(manaConsumed));
        
        return true;
    }

    public bool TriggerQuickRegen()
    {
        if (playerMana != null && !playerMana.TryUseMana(regenManaCost)) return false;
        if (playerHealth == null) return false;

        if (regenRoutine != null) StopCoroutine(regenRoutine);
        regenRoutine = StartCoroutine(PerformQuickRegen());
        
        return true;
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
        isTimeStopActive = true; 

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
        isTimeStopActive = false; 
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

    private IEnumerator PerformQuickRegen()
    {
        Debug.Log("Quick Regen Started!");
        
        int ticks = 10;
        int healPerTick = regenTotalAmount / ticks;
        float timeBetweenTicks = regenDuration / ticks;

        for (int i = 0; i < ticks; i++)
        {
            playerHealth.Heal(healPerTick);
            yield return new WaitForSecondsRealtime(timeBetweenTicks); 
        }

        Debug.Log("Quick Regen Finished.");
    }

    public bool TriggerManaBurst()
    {
        if (playerMana == null) return false;

        if (manaBurstRoutine != null) StopCoroutine(manaBurstRoutine);
        manaBurstRoutine = StartCoroutine(PerformManaBurst());
        
        return true;
    }

    private IEnumerator PerformManaBurst()
    {
        Debug.Log("Mana Burst Casted! Restoring mana...");
        
        int ticks = 10;
        float manaPerTick = manaBurstTotalAmount / ticks;
        float timeBetweenTicks = manaBurstDuration / ticks;

        for (int i = 0; i < ticks; i++)
        {
            playerMana.RestoreMana(manaPerTick);
            yield return new WaitForSecondsRealtime(timeBetweenTicks); 
        }

        Debug.Log("Mana Burst Finished.");
    }

    public bool TriggerSafeFall()
    {
        if (playerMana != null && !playerMana.TryUseMana(safeFallManaCost)) return false;
        if (playerHealth == null) return false;

        if (safeFallRoutine != null) StopCoroutine(safeFallRoutine);
        safeFallRoutine = StartCoroutine(PerformSafeFall());
        
        return true;
    }

    private IEnumerator PerformSafeFall()
    {
        Debug.Log($"Safe Fall Active! Immune to fall damage for {safeFallDuration} seconds.");
        
        playerHealth.isFallDamageImmune = true;

        float timer = 0f;
        while (timer < safeFallDuration)
        {
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        playerHealth.isFallDamageImmune = false;
        Debug.Log("Safe Fall has worn off!");
    }

    public bool TriggerSpeedBoost()
    {
        if (playerMana != null && !playerMana.TryUseMana(speedBoostManaCost)) return false;

        if (speedBoostRoutine != null) StopCoroutine(speedBoostRoutine);
        speedBoostRoutine = StartCoroutine(PerformSpeedBoost());
        
        return true;
    }

    private IEnumerator PerformSpeedBoost()
    {
        Debug.Log($"Speed Boost Active! Speed multiplied by {speedBoostMultiplier} for {speedBoostDuration} seconds.");
        
        playerMovement.activeSpeedMultiplier = speedBoostMultiplier;

        float timer = 0f;
        while (timer < speedBoostDuration)
        {
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        playerMovement.activeSpeedMultiplier = 1f;
        Debug.Log("Speed Boost has worn off.");
    }


    public bool TriggerNightLight()
    {
        if (playerMana != null && !playerMana.TryUseMana(lightManaCost)) return false;

        if (lightRoutine != null) StopCoroutine(lightRoutine);
        lightRoutine = StartCoroutine(PerformNightLight());
        
        return true;
    }

    private IEnumerator PerformNightLight()
    {
        Debug.Log($"Night Light Casted! Illuminating area for {lightDuration} seconds.");

        // Create the light if it doesn't exist yet
        if (activeLight == null)
        {
            activeLight = new GameObject("PlayerMagicalLight");
            
            // Parent it to the main camera so it follows where the player looks
            activeLight.transform.SetParent(Camera.main.transform);
            activeLight.transform.localPosition = Vector3.zero;

            Light lightComponent = activeLight.AddComponent<Light>();
            lightComponent.type = LightType.Point; // Change to LightType.Spot for a flashlight effect
            lightComponent.color = lightColor;
            lightComponent.intensity = lightIntensity;
            lightComponent.range = lightRange;
        }

        activeLight.SetActive(true);

        float timer = 0f;
        while (timer < lightDuration)
        {
            timer += Time.unscaledDeltaTime; 
            yield return null;
        }

        activeLight.SetActive(false);
        Debug.Log("Night Light has faded.");
    }
}