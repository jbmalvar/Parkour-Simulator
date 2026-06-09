using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))] // Ensures the GameObject always has an AudioSource
public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    public float maxMana = 100f;
    public float regenRate = 5f; // How much mana comes back per second
    private float currentMana;

    [Header("UI")]
    public Image manaBarFill;

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Sound played when attempting to cast without enough mana.")]
    public AudioClip outOfManaSound;

    void Start()
    {
        currentMana = maxMana;
        UpdateUI();

        // Auto-grab the AudioSource if it wasn't assigned in the inspector
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
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
        
        // ---> NEW: Play out of mana sound <---
        if (audioSource != null && outOfManaSound != null)
        {
            // PlayOneShot prevents the sound from clipping if the player spams the button
            audioSource.PlayOneShot(outOfManaSound);
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

    // Drains everything and returns the amount taken to scale abilities
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