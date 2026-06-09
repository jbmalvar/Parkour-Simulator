using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelTransition : MonoBehaviour
{
    public string nextLevelName;
    public SpeedrunTimer levelTimer;
    public int LevelNumber;
    public TextMeshProUGUI winText;
    private bool hasFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasFinished)
        {
            hasFinished = true;

            if (winText != null)
                winText.gameObject.SetActive(true);

            if (LevelNumber > 0)
            {
                float finalTime = levelTimer.StopTimer();
                string player = PlayerPrefs.GetString(UsernameManager.SavedNamePrefKey, "Anonymous");
                print("reached");
                LeaderboardManager.Instance.SubmitScore(LevelNumber, player, finalTime);
            }

            MenuManager.UnlockNextLevel(LevelNumber);
            Invoke("LoadNextScene", 3f); // wait 3 seconds then load
        }
    }

    void LoadNextScene()
    {
        if (nextLevelName != "")
            SceneManager.LoadScene(nextLevelName);
    }
}