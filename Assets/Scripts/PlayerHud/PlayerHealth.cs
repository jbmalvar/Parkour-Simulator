using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Effects")]
    public CameraShake cameraShake; 
    public Image damageOverlay; 
    public float fadeSpeed = 1.5f; 

    [Header("Audio")]
    public AudioSource audioSource; // The speaker
    public AudioClip hurtSound;     // Sound when taking normal damage
    public AudioClip deathSound;    // Sound when health hits 0

    void Awake()
    {
        // Force the script to turn back on (in case the death sequence left it off)
        this.enabled = true;

        currentHealth = maxHealth;
        
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 0); 
        }
    }

    void Update()
    {
        if (damageOverlay != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            
            float minAlpha = 0f;
            if (healthPercent < 0.5f)
            {
                minAlpha = 0.5f - healthPercent; 
            }

            Color currentColor = damageOverlay.color;

            if (currentColor.a > minAlpha)
            {
                currentColor.a -= fadeSpeed * Time.deltaTime;
                currentColor.a = Mathf.Max(currentColor.a, minAlpha); 
            }
            else if (currentColor.a < minAlpha)
            {
                currentColor.a += fadeSpeed * Time.deltaTime;
                currentColor.a = Mathf.Min(currentColor.a, minAlpha);
            }

            damageOverlay.color = currentColor;
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        
        Debug.Log($"Ouch! Took {damageAmount} damage. Current Health: {currentHealth}");

        // ---> NEW: Play the hurt sound! <---
        if (audioSource != null && hurtSound != null && currentHealth > 0)
        {
            // PlayOneShot lets multiple sounds overlap without cutting each other off
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
        
        // ---> NEW: Play the death sound! <---
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

        yield return new WaitForSeconds(1f);

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}