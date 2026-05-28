using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 spawnOffset = new Vector3(0, 0.1f, 0);

    [Header("Visuals")]
    public Renderer beamRenderer;
    public ParticleSystem activateEffect;

    // Mirror's Edge: bright red when active, dim white when not
    public Color activeColor = new Color(1f, 0.15f, 0.05f);
    public Color inactiveColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

    private bool isActive = false;
    private static Checkpoint currentCheckpoint;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        SetVisual(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive && IsPlayer(other))
            Activate(GetPlayerRoot(other));
    }

    // Robust player detection: works via tag OR the PlayerMovement component,
    // so a missing/incorrect "Player" tag won't silently break checkpoints.
    public static bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player")
            || other.GetComponentInParent<PlayerMovement>() != null;
    }

    static Transform GetPlayerRoot(Collider other)
    {
        var movement = other.GetComponentInParent<PlayerMovement>();
        return movement != null ? movement.transform : other.transform;
    }

    private void Activate(Transform player)
    {
        if (currentCheckpoint != null && currentCheckpoint != this)
            currentCheckpoint.Deactivate();

        isActive = true;
        currentCheckpoint = this;

        // Respawn where the player actually crossed (guaranteed valid ground)
        // rather than at the beacon's center, which may float in the air.
        Vector3 spawnPos = (player != null ? player.position : transform.position) + spawnOffset;
        Quaternion spawnRot = player != null ? player.rotation : transform.rotation;
        CheckpointManager.Instance?.SetCheckpoint(spawnPos, spawnRot);

        SetVisual(true);

        if (activateEffect != null)
            activateEffect.Play();
    }

    private void Deactivate()
    {
        isActive = false;
        SetVisual(false);
    }

    private void SetVisual(bool active)
    {
        if (beamRenderer == null) return;

        // Works with both standard and URP/HDRP emissive materials
        Material mat = beamRenderer.material;
        Color c = active ? activeColor : inactiveColor;
        mat.color = c;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", active ? c * 2f : Color.black);
        }
    }

    // Gizmo so checkpoints are visible in the editor
    void OnDrawGizmos()
    {
        Gizmos.color = isActive ? new Color(1f, 0.15f, 0.05f, 0.7f) : new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
    }
}
