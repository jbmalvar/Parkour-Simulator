using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; 

public class LeaderboardDisplay : MonoBehaviour
{
    public GameObject viewPanel;
    public TextMeshProUGUI boardText;
    public int currentLevelID = 1;
    private bool isVisible = false;

    private void Start() 
    { 
        viewPanel.SetActive(false); 
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleBoard();
        }
    }

    private void ToggleBoard()
    {
        isVisible = !isVisible;
        viewPanel.SetActive(isVisible);
        
        if (isVisible)
        {
            boardText.text = "Loading...";
            LeaderboardManager.Instance.FetchLeaderboard(currentLevelID, PopulateUI);
        }
    }

    private void PopulateUI(ScoreData[] records)
    {
        if (records == null || records.Length == 0)
        {
            boardText.text = "No records yet."; 
            return;
        }

        string outText = $"TOP 5 - LEVEL {currentLevelID}\n\n";
        for (int i = 0; i < records.Length; i++)
        {
            outText += $"{i + 1}. {records[i].playerName} - {records[i].timeSpent.ToString("F2")}s\n";
        }
        boardText.text = outText;
    }
}