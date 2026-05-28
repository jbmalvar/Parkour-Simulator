using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject landingPanel;
    public GameObject levelSelectPanel;
    public GameObject aboutPanel;
    public GameObject settingsPanel;
    public GameObject levelConfirmPanel;

    [Header("Level Confirm")]
    public TMPro.TextMeshProUGUI confirmLevelNameText;

    [Header("Player (disable during menu)")]
    public GameObject playerObject;      // the Player root
    public Camera menuCamera;            // drag Main Camera here

    // ── Update these to match your actual scene names in Build Settings ──
    private static readonly string[] LevelSceneNames =
    {
        "Tutorial (Index 0)",
        "MovementScene",
        "Level3"           // placeholder — rename when you add the 3rd scene
    };

    public static readonly string[] LevelDisplayNames =
    {
        "Level 1 — Tutorial",
        "Level 2 — Movement",
        "Level 3 — ???"
    };

    private readonly Stack<GameObject> panelStack = new Stack<GameObject>();
    private int pendingLevelIndex = -1;
    private Transform originalCameraParent;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Unlock cursor immediately — before camera scripts can lock it
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Start()
    {
        ShowPanel(landingPanel);
        SetPlayerActive(false);
    }

    // ── Navigation ──────────────────────────────────────────────────────

    public void ShowLevelSelect() => PushPanel(levelSelectPanel);
    public void ShowAbout()       => PushPanel(aboutPanel);
    public void ShowSettings()    => PushPanel(settingsPanel);

    public void GoBack()
    {
        if (panelStack.Count <= 1) return;
        panelStack.Pop().SetActive(false);
        panelStack.Peek().SetActive(true);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Level Select ─────────────────────────────────────────────────────

    public void OnLevelClicked(int index)
    {
        if (!IsLevelUnlocked(index)) return;

        pendingLevelIndex = index;

        if (confirmLevelNameText != null)
            confirmLevelNameText.text = LevelDisplayNames[index];

        PushPanel(levelConfirmPanel);
    }

    public void ConfirmStartLevel()
    {
        if (pendingLevelIndex < 0 || pendingLevelIndex >= LevelSceneNames.Length) return;
        SetPlayerActive(true);
        SceneManager.LoadScene(LevelSceneNames[pendingLevelIndex]);
    }

    public void CancelLevelConfirm()
    {
        GoBack();
        pendingLevelIndex = -1;
    }

    // ── Unlock Helpers (static so LevelComplete can call them) ────────────

    public static bool IsLevelUnlocked(int index)
    {
        if (index == 0) return true;
        return PlayerPrefs.GetInt($"Level_{index}_Unlocked", 0) == 1;
    }

    public static void UnlockNextLevel(int completedLevelIndex)
    {
        int next = completedLevelIndex + 1;
        if (next < LevelSceneNames.Length)
        {
            PlayerPrefs.SetInt($"Level_{next}_Unlocked", 1);
            PlayerPrefs.Save();
        }
    }

    // ── Player Control ───────────────────────────────────────────────────

    private void SetPlayerActive(bool active)
    {
        if (!active && menuCamera != null)
        {
            // Detach camera from player BEFORE disabling so it stays alive
            originalCameraParent = menuCamera.transform.parent;
            menuCamera.transform.SetParent(null);

            // Disable all MonoBehaviours on the camera so it stops rotating
            foreach (var mb in menuCamera.GetComponents<MonoBehaviour>())
                mb.enabled = false;
        }

        if (playerObject != null) playerObject.SetActive(active);

        if (active && menuCamera != null && originalCameraParent != null)
        {
            // Re-attach and re-enable camera scripts when game starts
            menuCamera.transform.SetParent(originalCameraParent);
            foreach (var mb in menuCamera.GetComponents<MonoBehaviour>())
                mb.enabled = true;
        }

        Cursor.lockState = active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !active;
    }

    // ── Internal ─────────────────────────────────────────────────────────

    private void PushPanel(GameObject panel)
    {
        if (panelStack.Count > 0)
            panelStack.Peek().SetActive(false);
        panelStack.Push(panel);
        panel.SetActive(true);
    }

    private void ShowPanel(GameObject panel)
    {
        panelStack.Clear();

        landingPanel.SetActive(false);
        levelSelectPanel.SetActive(false);
        aboutPanel.SetActive(false);
        settingsPanel.SetActive(false);
        levelConfirmPanel.SetActive(false);

        panelStack.Push(panel);
        panel.SetActive(true);
    }
}
