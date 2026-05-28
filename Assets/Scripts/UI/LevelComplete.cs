using UnityEngine;
using UnityEngine.SceneManagement;

// Place this on a trigger volume at the end of each level (finish line).
// Set thisLevelIndex to match the level (0, 1, or 2).
[RequireComponent(typeof(Collider))]
public class LevelComplete : MonoBehaviour
{
    public int thisLevelIndex;

    void Start() => GetComponent<Collider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            Complete();
    }

    // Can also be called directly (e.g., from a UI button for testing)
    public void Complete()
    {
        UIManager.UnlockNextLevel(thisLevelIndex);
        SceneManager.LoadScene("MainMenu");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.3f, 0.35f);
        Gizmos.DrawCube(transform.position, transform.localScale);
    }
}
