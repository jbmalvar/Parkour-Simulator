using UnityEngine;
using UnityEngine.InputSystem;

public class TimeController : MonoBehaviour
{
    [Header("Time Control Settings")]
    [Range(0f, 1f)]
    public float slowMotionFactor = 0.1f;
    private float originalFixedDeltaTime;

    void Start()
    {
        originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            StartTimeSlow();
        }
        
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            StopTimeSlow();
        }
    }

    private void StartTimeSlow()
    {
        Time.timeScale = slowMotionFactor;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionFactor;
    }

    private void StopTimeSlow()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }


    private void OnDestroy()
    {
        // Ensure time scale is reset when the object is destroyed
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }
}
