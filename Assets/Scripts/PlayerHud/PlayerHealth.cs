using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("UI")]
    public Image healthBarFill; // The colored bar that shrinks
    public float healthBarSpeed = 10f; // How fast the bar slides to the new value

    [Header("Effects")]
    public CameraShake cameraShake; 
    public Image damageOverlay; 
    public float fadeSpeed = 1.5f; 

    [Header("Audio")]
    public AudioSource audioSource; 
    public AudioClip hurtSound;     
    public AudioClip deathSound;    

    [Header("Buffs")]
    public bool isFallDamageImmune = false;

    void Awake()
    {
        this.enabled = true;
        currentHealth = maxHealth;
        
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 0); 
        }

        // Snap the health bar to full instantly when the game starts
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f; 
        }
    }

    void Update()
    {
        float healthPercent = (float)currentHealth / maxHealth;

        // ---> NEW: Smoothly animate the health bar <---
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, healthPercent, healthBarSpeed * Time.unscaledDeltaTime);
        }

        // Handle the red damage screen
        if (damageOverlay != null)
        {
            float minAlpha = 0f;
            if (healthPercent < 0.5f)
            {
                minAlpha = 0.5f - healthPercent; 
            }

            Color currentColor = damageOverlay.color;

            if (currentColor.a > minAlpha)
            {
                currentColor.a -= fadeSpeed * Time.unscaledDeltaTime;
                currentColor.a = Mathf.Max(currentColor.a, minAlpha); 
            }
            else if (currentColor.a < minAlpha)
            {
                currentColor.a += fadeSpeed * Time.unscaledDeltaTime;
                currentColor.a = Mathf.Min(currentColor.a, minAlpha);
            }

            damageOverlay.color = currentColor;
        }
    }

    // ---> NEW: Dedicated method for fall damage <---
    public void TakeFallDamage(int damageAmount)
    {
        if (isFallDamageImmune)
        {
            Debug.Log("Safe Fall absorbed the impact! 0 damage taken.");
            return; // Exit out before applying any damage
        }
        
        // If the spell is NOT active, take damage normally
        TakeDamage(damageAmount);
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        
        Debug.Log($"Ouch! Took {damageAmount} damage. Current Health: {currentHealth}");

        if (audioSource != null && hurtSound != null && currentHealth > 0)
        {
            audioSource.PlayOneShot(hurtSound);
        }

        if (cameraShake != null)
        {
            StartCoroutine(cameraShake.Shake(0.2f, 0.4f));
        }

        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 0.6f); 
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        
    }

    private bool isDying = false;

    private void Die()
    {
        if (isDying) return;          // don't re-trigger mid-respawn
        isDying = true;

        Debug.Log("Player Died! Respawning at last checkpoint...");

        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 1f);
        }

        // Respawn at the last checkpoint (handles fade + teleport) instead of reloading
        // the whole level back to the start.
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.TriggerDeath();
            yield return new WaitForSecondsRealtime(0.5f);   // let the fade hide the teleport

            currentHealth = maxHealth;
            if (healthBarFill != null) healthBarFill.fillAmount = 1f;
            if (damageOverlay != null) damageOverlay.color = new Color(1, 0, 0, 0f);
            isDying = false;
        }
        else
        {
            // Fallback if there is no CheckpointManager in the scene.
            yield return new WaitForSecondsRealtime(1f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    // ---> NEW: Method to handle incoming healing <---
    public void Heal(int healAmount)
    {
        if (currentHealth <= 0) return; // No reviving from the dead!
        
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        Debug.Log($"Healed for {healAmount}. Current Health: {currentHealth}");
    }
}