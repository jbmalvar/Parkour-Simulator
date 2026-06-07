using UnityEngine;
using UnityEngine.UI;

public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    public float maxMana = 100f;
    public float regenRate = 5f; // How much mana comes back per second
    private float currentMana;

    [Header("UI")]
    public Image manaBarFill;

    void Start()
    {
        currentMana = maxMana;
        UpdateUI();
    }

    void Update()
    {
        // Passively regenerate mana over time
        if (currentMana < maxMana)
        {
            // Using unscaledDeltaTime so it regens even if you freeze time!
            currentMana += regenRate * Time.unscaledDeltaTime; 
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            UpdateUI();
        }
    }

    // The ability script will call this to check/drain mana
    public bool TryUseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateUI();
            return true; // Successfully cast the spell
        }
        
        Debug.Log("Not enough mana!");
        return false; // Spell failed
    }

    private void UpdateUI()
    {
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = currentMana / maxMana;
        }
    }

    // ---> NEW: Drains everything and returns the amount taken to scale abilities <---
    public float DrainAllMana()
    {
        float manaDrained = currentMana;
        currentMana = 0f;
        UpdateUI();
        return manaDrained;
    }

    public void RestoreMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        UpdateUI();
        
        Debug.Log($"Mana Restored by {amount}! Current Mana: {currentMana}");
    }
}