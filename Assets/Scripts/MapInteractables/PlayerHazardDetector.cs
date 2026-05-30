using UnityEngine;

public class PlayerHazardDetector : MonoBehaviour
{
    private PressurePlate currentPlate = null;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("PlayerHazardDetector triggered by: " + other.gameObject.name);

        Hazard hazard = other.GetComponent<Hazard>();
        if (hazard != null)
        {
            CheckpointManager.Instance?.TriggerDeath();
        }

        PressurePlate plate = other.GetComponent<PressurePlate>();
        if (plate != null)
        {
            currentPlate = plate;
            plate.ActivatePlate();
        }

        CheckpointNotification checkpoint = other.GetComponent<CheckpointNotification>();
        if (checkpoint != null)
            checkpoint.ShowNotification();
    }

    void OnTriggerExit(Collider other)
    {
        PressurePlate plate = other.GetComponent<PressurePlate>();
        if (plate != null && plate == currentPlate)
        {
            plate.DeactivatePlate();
            currentPlate = null;
        }
    }
}