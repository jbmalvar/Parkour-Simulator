using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;

    private float progress = 0f;
    private bool movingToB = true;
    private Transform playerOnPlatform = null;
    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }

    void Update()
    {
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

        // Move player with platform
        if (playerOnPlatform != null)
        {
            Vector3 delta = transform.position - lastPosition;
            playerOnPlatform.GetComponent<CharacterController>().Move(delta);
        }

        lastPosition = transform.position;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerOnPlatform = other.transform;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerOnPlatform = null;
    }
}