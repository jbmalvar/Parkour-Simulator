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

    void Start()
    {
        currentHealth = maxHealth;
        
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 0); // Sets the Red alpha to 0 (invisible)
        }
    }

    void Update()
    {
        if (damageOverlay != null)
        {
            // 1. Calculate how low the health is (1.0 is full, 0.0 is dead)
            float healthPercent = (float)currentHealth / maxHealth;
            
            // 2. Set a minimum redness. If health is below 50% (0.5f), it starts staying red.
            // At 10% health, the minAlpha will be 0.4f (pretty dark red).
            float minAlpha = 0f;
            if (healthPercent < 0.5f)
            {
                minAlpha = 0.5f - healthPercent; 
            }

            Color currentColor = damageOverlay.color;

            // 3. If we just took a hit, the screen will be very red. Fade it down to the minAlpha.
            if (currentColor.a > minAlpha)
            {
                currentColor.a -= fadeSpeed * Time.deltaTime;
                // Clamp it so it doesn't fade away completely if health is low
                currentColor.a = Mathf.Max(currentColor.a, minAlpha); 
            }
            // 4. If we heal, slowly clear the red away
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

        if (cameraShake != null)
        {
            StartCoroutine(cameraShake.Shake(0.2f, 0.4f));
        }

        if (damageOverlay != null)
        {
            // Flash the screen hard red (0.6 opacity) upon impact
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
        
        // Stop the normal Update loop from fading the red away
        this.enabled = false; 
        
        // Start the death delay sequence
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        // 1. Force the screen to go completely solid red (Alpha = 1f)
        if (damageOverlay != null)
        {
            damageOverlay.color = new Color(1, 0, 0, 1f); 
        }

        // 2. Wait for 1 second in real-time so the player realizes they died
        yield return new WaitForSeconds(1f);

        // 3. NOW reload the level
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}