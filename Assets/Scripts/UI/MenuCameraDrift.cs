using UnityEngine;

// Gives the menu camera a slow, looping drift so the background feels alive instead
// of static. Put it on the Main Menu camera, point the camera at the scene you want
// to show, and it gently sways/pans around that starting pose.
//
// Uses unscaled time, so it keeps moving even though the menu may set Time.timeScale.
public class MenuCameraDrift : MonoBehaviour
{
    [Header("Position sway (world units)")]
    public float positionAmplitude = 1.2f;
    public float positionSpeed = 0.15f;

    [Header("Look sway (degrees)")]
    public float rotationAmplitude = 2.5f;
    public float rotationSpeed = 0.10f;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start() => ResetBase();

    // Re-capture the pose to sway around. MenuBackground calls this after it moves the
    // camera to frame the loaded scene, so the drift centres on the new vantage.
    public void ResetBase()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void Update()
    {
        float t = Time.unscaledTime;

        Vector3 offset = new Vector3(
            Mathf.Sin(t * positionSpeed) * positionAmplitude,
            Mathf.Sin(t * positionSpeed * 0.6f) * positionAmplitude * 0.4f,
            0f);
        transform.position = startPos + startRot * offset;

        float yaw   = Mathf.Sin(t * rotationSpeed) * rotationAmplitude;
        float pitch = Mathf.Cos(t * rotationSpeed * 0.8f) * rotationAmplitude * 0.5f;
        transform.rotation = startRot * Quaternion.Euler(pitch, yaw, 0f);
    }
}
