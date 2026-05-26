using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    public GameObject door;
    private bool isPressed = false;

    public void ActivatePlate()
    {
        if (!isPressed)
        {
            isPressed = true;
            transform.localScale -= new Vector3(0, 0.05f, 0);
            if (door != null)
                door.SetActive(false);
        }
    }

    public void DeactivatePlate()
    {
        isPressed = false;
        transform.localScale += new Vector3(0, 0.05f, 0);
        if (door != null)
            door.SetActive(true);
    }
}