using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WinTrigger : MonoBehaviour
{
    public TextMeshProUGUI winText;
    public string nextLevelName;
    public SpeedrunTimer levelTimer;
    public int LevelNumber;
    private bool hasFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasFinished)
        {
            hasFinished = true;

            if (winText != null)
                winText.gameObject.SetActive(true);

            if (LevelNumber > 0 && levelTimer != null)
            {
                float finalTime = levelTimer.StopTimer();
                string player = PlayerPrefs.GetString(UsernameManager.SavedNamePrefKey, "Anonymous");
                // TODO: save finalTime + player to leaderboard
            }

            MenuManager.UnlockNextLevel(LevelNumber);
            Invoke("LoadNextScene", 3f);
        }
    }

    private void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
            SceneManager.LoadScene(nextLevelName);
        else
            Debug.LogWarning("WinTrigger: nextLevelName is not set!");
    }
}