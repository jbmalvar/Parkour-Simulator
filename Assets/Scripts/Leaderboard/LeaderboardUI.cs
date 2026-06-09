using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Leaderboard Elements")]
    public Transform entriesContainer;
    // public GameObject scoreEntryPrefab; 
    public TextMeshProUGUI[] scoreTextSlots;
    public TextMeshProUGUI levelTitleText;

    private int currentDisplayLevel = 1; 

    // void OnEnable() 
    // {
    //     LoadLevelData(currentDisplayLevel);
    // }

    public void NextLevel()
    {
        if (currentDisplayLevel < 5)
        {
            currentDisplayLevel++;
            LoadLevelData(currentDisplayLevel);
        }
    }

    public void PreviousLevel()
    {
        if (currentDisplayLevel > 1)
        {
            currentDisplayLevel--;
            LoadLevelData(currentDisplayLevel);
        }
    }

    private void LoadLevelData(int level)
    {
        levelTitleText.text = $"Loading Level {level}...";
        foreach (var textSlot in scoreTextSlots)
        {
            textSlot.text = "---";
        }

        LeaderboardManager.Instance.FetchLeaderboard(level, OnScoresReceived);
    }

    private void OnScoresReceived(ScoreData[] scores)
    {
        levelTitleText.text = $"Level {currentDisplayLevel} Top Times";

        if (scores == null || scores.Length == 0)
        {
            scoreTextSlots[0].text = "No times recorded yet!";
            return;
        }

        // Loop through our slots and the database scores at the same time
        for (int i = 0; i < scoreTextSlots.Length; i++)
        {
            if (i < scores.Length) 
            {
                scoreTextSlots[i].text = $"{i + 1}. {scores[i].playerName} - {scores[i].timeSpent:F2}s";
            }
            else 
            {
                scoreTextSlots[i].text = $"{i + 1}. ---"; 
            }
        }
    }
}