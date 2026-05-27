using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialText;

    private string currentActivePromptID = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Call this to show text. It registers the ID of the prompt.
    public void ShowPrompt(string message, string promptID)
    {
        currentActivePromptID = promptID;
        tutorialText.text = message;
        tutorialPanel.SetActive(true);
    }

    // Call this to hide text. It verifies the ID so it doesn't accidentally hide a newer prompt.
    public void HidePrompt(string promptID)
    {
        if (currentActivePromptID == promptID)
        {
            tutorialPanel.SetActive(false);
            currentActivePromptID = "";
        }
    }
}