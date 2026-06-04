using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    // We use localPosition so it shakes relative to wherever the camera currently is,
    // which is crucial if your camera is parented to the player or a rig.
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null; // Wait until the next frame before continuing the loop
        }

        // Snap the camera back to its exact original position when done
        transform.localPosition = originalPos;
    }
}