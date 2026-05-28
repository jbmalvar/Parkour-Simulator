using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to each level button in the Level Select panel.
// Set levelIndex to 0, 1, or 2.
public class LevelButtonUI : MonoBehaviour
{
    [Header("Settings")]
    public int levelIndex;

    [Header("References")]
    public Button button;
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI lockLabel;    // Shows "LOCKED" when unavailable
    public GameObject lockedOverlay;    // Optional dim/grey overlay image

    void Awake()
    {
        // Self-wire so the button works without manual Inspector OnClick setup
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    void OnEnable() => Refresh();

    public void Refresh()
    {
        bool unlocked = MenuManager.IsLevelUnlocked(levelIndex);

        if (button != null)
            button.interactable = unlocked;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);

        if (lockLabel != null)
            lockLabel.text = unlocked ? "" : "LOCKED";

        if (levelNameText != null && levelIndex < MenuManager.LevelDisplayNames.Length)
            levelNameText.text = MenuManager.LevelDisplayNames[levelIndex];
    }

    // Wire this to the Button's OnClick event in the Inspector
    public void OnClick()
    {
        MenuManager.Instance?.OnLevelClicked(levelIndex);
    }
}
