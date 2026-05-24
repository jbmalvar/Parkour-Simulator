using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public GameObject door;
    private bool isPressed = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isPressed)
        {
            isPressed = true;
            ActivatePlate();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPressed = false;
            DeactivatePlate();
        }
    }

    void ActivatePlate()
    {
        // Press plate down visually
        transform.localScale -= new Vector3(0, 0.05f, 0);
        // Open the door
        if (door != null)
            door.SetActive(false);
    }

    void DeactivatePlate()
    {
        // Reset plate visually
        transform.localScale += new Vector3(0, 0.05f, 0);
        // Close the door
        if (door != null)
            door.SetActive(true);
    }
}