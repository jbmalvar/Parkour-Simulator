using UnityEngine;

public class Hazard : MonoBehaviour
{
    public enum HazardType { DeathPit, Spike, Lava }
    public HazardType hazardType;

    public float lavaDamageRate = 10f;
    public float maxHealth = 100f;

    private float currentHealth;
    private bool isInLava = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (hazardType == HazardType.Lava && isInLava)
        {
            currentHealth -= lavaDamageRate * Time.deltaTime;
            if (currentHealth <= 0)
                TriggerDeath();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        switch (hazardType)
        {
            case HazardType.DeathPit:
                TriggerDeath();
                break;
            case HazardType.Spike:
                TriggerDeath();
                break;
            case HazardType.Lava:
                isInLava = true;
                break;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (hazardType == HazardType.Lava)
        {
            isInLava = false;
            currentHealth = maxHealth;
        }
    }

    void TriggerDeath()
    {
        // This will connect to James's respawn system later
        Debug.Log("Player died from: " + hazardType.ToString());
        isInLava = false;
        currentHealth = maxHealth;
    }
}