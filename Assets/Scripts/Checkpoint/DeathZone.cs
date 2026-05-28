using UnityEngine;

// Place this on a large trigger volume below the map.
// Also used for any kill zone (lava, void, etc.)
[RequireComponent(typeof(Collider))]
public class DeathZone : MonoBehaviour
{
    void Start() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (Checkpoint.IsPlayer(other))
            CheckpointManager.Instance?.TriggerDeath();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
