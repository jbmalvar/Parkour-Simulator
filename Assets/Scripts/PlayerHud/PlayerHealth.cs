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

    private void Die()
    {
        Debug.Log("Player Died! Restarting level in 1 second...");
        
        if (audioSource != null && deathSound != null)
        {
            audioSource.PlayOneShot(deathSound);
        }

        this.enabled = false; 
        
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 1f); 
        }

        yield return new WaitForSecondsRealtime(1f);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
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