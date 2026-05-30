using UnityEngine;

public class CrushingBoulder : MonoBehaviour
{
    public float rollSpeed = 5f;
    public float resetTime = 3f;
    public Transform startPosition;

    private Rigidbody rb;
    private bool isRolling = true;
    private float resetTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
    }

    void Update()
    {
        if (!isRolling)
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= resetTime)
                ResetBoulder();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CheckpointManager.Instance?.TriggerDeath();
        }

        if (other.CompareTag("Wall"))
        {
            isRolling = false;
            resetTimer = 0f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void ResetBoulder()
    {
        transform.position = startPosition.position;
        transform.rotation = Quaternion.identity;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isRolling = true;
        resetTimer = 0f;
    }
}