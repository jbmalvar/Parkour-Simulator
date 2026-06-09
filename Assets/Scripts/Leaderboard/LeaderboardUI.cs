using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject leaderboardPanel;

    [Header("Leaderboard Elements")]
    public Transform entriesContainer;
    public GameObject scoreEntryPrefab; // Drag your text prefab here
    public TextMeshProUGUI levelTitleText;

    private int currentDisplayLevel = 1; // Starts by showing Level 1

    public void OpenLeaderboard()
    {
        mainMenuPanel.SetActive(false);
        leaderboardPanel.SetActive(true);
        LoadLevelData(currentDisplayLevel);
    }

    public void CloseLeaderboard()
    {
        leaderboardPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }


    public void NextLevel()
    {
        currentDisplayLevel++;
        LoadLevelData(currentDisplayLevel);
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
        
        foreach (Transform child in entriesContainer)
        {
            Destroy(child.gameObject);
        }


        LeaderboardManager.Instance.FetchLeaderboard(level, OnScoresReceived);
    }


    private void OnScoresReceived(ScoreData[] scores)
    {
        levelTitleText.text = $"Level {currentDisplayLevel} Top Times";

    
        if (scores == null || scores.Length == 0)
        {
            GameObject noDataObj = Instantiate(scoreEntryPrefab, entriesContainer);
            noDataObj.GetComponent<TextMeshProUGUI>().text = "No times recorded yet!";
            return;
        }

        for (int i = 0; i < scores.Length; i++)
        {
            GameObject entryObj = Instantiate(scoreEntryPrefab, entriesContainer);
            TextMeshProUGUI textMesh = entryObj.GetComponent<TextMeshProUGUI>();
            textMesh.text = $"{i + 1}. {scores[i].playerName} - {scores[i].timeSpent:F2}s";
        }
    }
}