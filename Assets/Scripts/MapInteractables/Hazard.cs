using UnityEngine;

public class Hazard : MonoBehaviour
{
    public enum HazardType { DeathPit, Spike, Lava }
    public HazardType hazardType;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TriggerDeath();
    }

    void TriggerDeath()
    {
        Respawn respawn = FindAnyObjectByType<Respawn>();
        if (respawn != null)
            respawn.PlayerDied();
    }
}