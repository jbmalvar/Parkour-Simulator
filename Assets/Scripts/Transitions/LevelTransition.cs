using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    [Tooltip("Type the exact name of the next level/scene here in the Inspector")]
    public string nextLevelName;

    // This function is called whenever another object enters this object's trigger collider
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that touched the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            // Load the next level
            Debug.Log("Player touched the portal! Loading level: " + nextLevelName);
            SceneManager.LoadScene(nextLevelName);
        }
    }
}
