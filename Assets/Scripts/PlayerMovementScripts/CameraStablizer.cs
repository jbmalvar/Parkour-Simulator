using UnityEngine;

public class CameraStabilizer : MonoBehaviour
{
    public Transform targetHeadBone; // Assign B-head here
    public Vector3 offset = new Vector3(0, 0.05f, 0.2f); // 0.2 forward helps see the chest

    void LateUpdate()
    {
        if (targetHeadBone != null)
        {
            // Follow position only
            // We use the player's rotation to calculate the local offset correctly
            transform.position = targetHeadBone.position + 
                                transform.right * offset.x + 
                                transform.up * offset.y + 
                                transform.forward * offset.z;
        }
    }
}