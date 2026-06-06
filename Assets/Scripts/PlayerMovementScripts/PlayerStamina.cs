using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 150f;
    public float sprintDrainRate = 10f; 
    public float regenRate = 15f;
    public float regenDelay = 1.5f; 

    private float currentStamina;
    private float lastStaminaUseTime;
    
    // ---> NEW: Flag to track if the spell is active <---
    private bool isInfiniteStaminaActive = false;

    [Header("UI")]
    public Image staminaBarFill;

    void Start()
    {
        currentStamina = maxStamina;
        UpdateUI();
    }

    void Update()
    {
        // ---> NEW: Keep it pegged at max while spell is active <---
        if (isInfiniteStaminaActive)
        {
            currentStamina = maxStamina;
            UpdateUI();
            return; // Skip normal regen logic
        }

        if (Time.time > lastStaminaUseTime + regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            UpdateUI();
        }
    }

    public void DrainStamina()
    {
        // ---> NEW: Completely ignore drain requests if spell is active <---
        if (isInfiniteStaminaActive) return;

        currentStamina -= sprintDrainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        lastStaminaUseTime = Time.time;
        UpdateUI();
    }

    public bool HasStamina()
    {
        // ---> NEW: Always say yes if spell is active <---
        if (isInfiniteStaminaActive) return true;
        
        return currentStamina > 0;
    }

    // ---> NEW: Method for PlayerAbilities to turn the spell ON/OFF <---
    public void SetInfiniteStamina(bool state)
    {
        isInfiniteStaminaActive = state;
        
        // If we just turned it on, visually fill the bar instantly
        if (state)
        {
            currentStamina = maxStamina;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }
    }
}