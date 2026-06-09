using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    private float progress = 0f;
    private bool movingToB = true;
    private Transform rider;
    private CharacterController riderController;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        if (movingToB)
        {
            progress += Time.deltaTime * speed;
            if (progress >= 1f) { progress = 1f; movingToB = false; }
        }
        else
        {
            progress -= Time.deltaTime * speed;
            if (progress <= 0f) { progress = 0f; movingToB = true; }
        }

        transform.position = Vector3.Lerp(pointA.position, pointB.position, progress);

        // Carry the player — but only if they're actually still standing on us. This
        // stops us from dragging a player who has stepped off OR who teleported away on
        // respawn (the teleport doesn't reliably fire OnTriggerExit, which caused the
        // "dead player keeps gliding with the platform" bug).
        Vector3 delta = transform.position - lastPosition;
        if (rider != null)
        {
            if (riderController != null && riderController.enabled && StillOnPlatform(rider))
                riderController.Move(delta);
            else
                ClearRider();
        }

        lastPosition = transform.position;
    }

    // Is the rider within our footprint (in local space, so it tracks our movement)?
    private bool StillOnPlatform(Transform r)
    {
        Vector3 local = transform.InverseTransformPoint(r.position);
        return Mathf.Abs(local.x) < 0.75f   // a bit past the platform edge
            && Mathf.Abs(local.z) < 0.75f
            && local.y > -1f && local.y < 8f; // above the deck, not teleported elsewhere
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            rider = other.transform;
            riderController = other.GetComponent<CharacterController>();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            ClearRider();
    }

    private void ClearRider()
    {
        rider = null;
        riderController = null;
    }
}
