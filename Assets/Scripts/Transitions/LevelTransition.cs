using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelTransition : MonoBehaviour
{
    public string nextLevelName;
    public SpeedrunTimer levelTimer;
    public int LevelNumber;
    private bool hasFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            hasFinished = true;
            if (LevelNumber > 0) {
                float finalTime = levelTimer.StopTimer();
                string player = PlayerPrefs.GetString(UsernameManager.SavedNamePrefKey, "Anonymous");
                LeaderboardManager.Instance.SubmitScore(LevelNumber, player, finalTime);
            }

            SceneManager.LoadScene(nextLevelName);
            // Debug.Log("Player touched the portal! Loading level: " + nextLevelName);
        }
    }
}