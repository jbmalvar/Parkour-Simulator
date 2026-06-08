using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransition : MonoBehaviour
{
    [Tooltip("Type the exact name of the next level/scene here in the Inspector")]
    public string nextLevelName;
    public SpeedrunTimer levelTimer; 
    private bool hasFinished = false;

    private void OnTriggerEnter(Collider other)
    {
     
        if (other.CompareTag("Player"))
        {
            // Send to DB with name and Time
            hasFinished = true;
            float finalTime = levelTimer.StopTimer();
            string player = PlayerPrefs.GetString(UsernameManager.SavedNamePrefKey, "Anonymous");
            LeaderboardManager.Instance.SubmitScore(1, player, finalTime);
            SceneManager.LoadScene(nextLevelName);
            // Debug.Log("Player touched the portal! Loading level: " + nextLevelName);
        }
    }
}
