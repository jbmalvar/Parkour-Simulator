using UnityEngine;
using UnityEngine.UI;

// Attach to any menu button and pick an action. The button wires itself to
// MenuManager at runtime — no Inspector OnClick wiring needed (and nothing to
// silently lose when an editor tool rebuilds the panel).
[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour
{
    public enum Action
    {
        Play,           // -> ShowLevelSelect
        About,          // -> ShowAbout
        Settings,       // -> ShowSettings
        Exit,           // -> ExitGame
        Back,           // -> GoBack
        StartLevel,     // -> ConfirmStartLevel
        CancelConfirm,   // -> CancelLevelConfirm
        Leaderboard
    }

    public Action action;

    void Awake()
    {
        var button = GetComponent<Button>();
        button.onClick.AddListener(Invoke);
    }

    public void Invoke()
    {
        var m = MenuManager.Instance;
        if (m == null) return;

        switch (action)
        {
            case Action.Play:          m.ShowLevelSelect();    break;
            case Action.About:         m.ShowAbout();          break;
            case Action.Settings:      m.ShowName();       break;
            case Action.Exit:          m.ExitGame();           break;
            case Action.Back:          m.GoBack();             break;
            case Action.StartLevel:    m.ConfirmStartLevel();  break;
            case Action.CancelConfirm: m.CancelLevelConfirm(); break;
            // case Action.Leaderboard:   m.ShowLeaderboard();    break;
        }
    }
}
