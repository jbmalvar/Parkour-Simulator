using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class PlayerMana : MonoBehaviour
{
    [Header("Mana Settings")]
    public float maxMana = 100f;
    public float regenRate = 5f;
    private float currentMana;

    [Header("UI")]
    public Image manaBarFill;

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Sound played when attempting to cast without enough mana.")]
    public AudioClip outOfManaSound;
    
    // We only need to track the internal time now
    private float nextAllowedSoundTime = 0f; 

    void Start()
    {
        currentMana = maxMana;
        UpdateUI();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (currentMana < maxMana)
        {
            currentMana += regenRate * Time.unscaledDeltaTime; 
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            UpdateUI();
        }
    }

    public bool TryUseMana(float amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateUI();
            return true;
        }
        
        if (audioSource != null && outOfManaSound != null)
        {
            // Only play if the current time is greater than the finish time of the last clip
            if (Time.time >= nextAllowedSoundTime)
            {
                audioSource.PlayOneShot(outOfManaSound);
                
                // ---> NEW: Automatically lock the sound for the exact length of the audio file <---
                nextAllowedSoundTime = Time.time + outOfManaSound.length;
            }
        }

        Debug.Log("Not enough mana!");
        return false;
    }

    private void UpdateUI()
    {
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = currentMana / maxMana;
        }
    }

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