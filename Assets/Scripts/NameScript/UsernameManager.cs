using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UsernameManager : MonoBehaviour
{
    public GameObject usernamePanel;
    public TMP_InputField nameInputField;
    public Button confirmButton;
    public const string SavedNamePrefKey = "PlayerSpeedrunName";

    private void Start()
    {
        confirmButton.onClick.AddListener(ProcessAndSaveName);
        if (PlayerPrefs.HasKey(SavedNamePrefKey))
        {
            nameInputField.text = PlayerPrefs.GetString(SavedNamePrefKey);
            usernamePanel.SetActive(false); 
        }
    }

    private void ProcessAndSaveName()
    {
        string input = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(input)) return;
        
        if (input.Length > 15) input = input.Substring(0, 15);
        
        PlayerPrefs.SetString(SavedNamePrefKey, input);
        PlayerPrefs.Save();
        usernamePanel.SetActive(false);
    }
}