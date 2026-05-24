using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform pointA; // drag empty GameObject here
    public Transform pointB; // drag empty GameObject here
    public float speed = 2f;

    private float progress = 0f;
    private bool movingToB = true;

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
    }

    // Player moves WITH the platform
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            other.transform.SetParent(null);
    }
}