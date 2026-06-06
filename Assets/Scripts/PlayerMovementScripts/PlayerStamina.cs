using UnityEngine;
using UnityEngine.UI;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina Settings")]
    public float maxStamina = 150f;
    public float sprintDrainRate = 10f; 
    public float regenRate = 15f;
    public float regenDelay = 1.5f; // How long after stopping before it recharges

    private float currentStamina;
    private float lastStaminaUseTime;

    [Header("UI")]
    public Image staminaBarFill;

    void Start()
    {
        currentStamina = maxStamina;
        UpdateUI();
    }

    void Update()
    {
        // Automatically regenerate stamina if enough time has passed since last use
        if (Time.time > lastStaminaUseTime + regenDelay && currentStamina < maxStamina)
        {
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            UpdateUI();
        }
    }

    // PlayerMovement will call this when sprinting
    public void DrainStamina()
    {
        currentStamina -= sprintDrainRate * Time.deltaTime;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        lastStaminaUseTime = Time.time;
        UpdateUI();
    }

    // A quick check so PlayerMovement knows if sprinting is allowed
    public bool HasStamina()
    {
        return currentStamina > 0;
    }

    private void UpdateUI()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = currentStamina / maxStamina;
        }
    }
}