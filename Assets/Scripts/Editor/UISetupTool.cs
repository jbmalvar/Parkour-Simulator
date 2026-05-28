using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public static class UISetupTool
{
    [MenuItem("Tools/Setup UI Panels")]
    public static void SetupAllPanels()
    {
        SetupLevelSelectPanel();
        SetupAboutPanel();
        SetupSettingsPanel();
        SetupLevelConfirmPanel();
        Debug.Log("✓ All panels set up!");
    }

    static void SetupLevelConfirmPanel()
    {
        GameObject panel = FindInactive("LevelConfirmPanel");
        if (panel == null) { Debug.LogError("LevelConfirmPanel not found!"); return; }

        ClearChildren(panel);
        AddVerticalLayout(panel, TextAnchor.MiddleCenter);

        CreateLabel(panel, "START LEVEL?", 40);

        // Level name text (filled at runtime by MenuManager)
        GameObject nameObj = CreateLabel(panel, "Level Name", 30);
        nameObj.name = "LevelTextName";

        // Wire Start and Back buttons to MenuManager
        MenuManager uiManager = FindMenuManager();

        GameObject startBtn = CreateButton(panel, "Start", 80);
        startBtn.name = "StartButton";
        if (uiManager != null)
            startBtn.GetComponent<Button>().onClick.AddListener(() => uiManager.ConfirmStartLevel());

        GameObject backBtn = CreateButton(panel, "Back", 60);
        backBtn.name = "BackButton";
        if (uiManager != null)
            backBtn.GetComponent<Button>().onClick.AddListener(() => uiManager.CancelLevelConfirm());

        // Assign the label to MenuManager's confirmLevelNameText field
        if (uiManager != null)
            uiManager.confirmLevelNameText = nameObj.GetComponent<TextMeshProUGUI>();

        EditorUtility.SetDirty(panel);
        if (uiManager != null) EditorUtility.SetDirty(uiManager);
    }

    // ── Level Select ─────────────────────────────────────────────────────

    static void SetupLevelSelectPanel()
    {
        GameObject panel = FindInactive("LevelSelectPanel");
        if (panel == null) { Debug.LogError("LevelSelectPanel not found in scene!"); return; }

        ClearChildren(panel);
        AddVerticalLayout(panel, TextAnchor.MiddleCenter);

        // Title
        CreateLabel(panel, "SELECT LEVEL", 48);

        // 3 level buttons
        string[] names = { "Level 1", "Level 2", "Level 3" };
        for (int i = 0; i < 3; i++)
        {
            GameObject btn = CreateButton(panel, names[i], 80);
            var levelBtn = btn.AddComponent<LevelButtonUI>();
            levelBtn.levelIndex = i;
            levelBtn.button = btn.GetComponent<Button>();
            levelBtn.levelNameText = btn.GetComponentInChildren<TextMeshProUGUI>();

            // Wire OnClick to LevelButtonUI.OnClick
            int captured = i;
            btn.GetComponent<Button>().onClick.AddListener(() => levelBtn.OnClick());
        }

        // Back button
        GameObject back = CreateButton(panel, "Back", 60);
        MenuManager uiManager = FindMenuManager();
        if (uiManager != null)
            back.GetComponent<Button>().onClick.AddListener(() => uiManager.GoBack());

        EditorUtility.SetDirty(panel);
    }

    // ── About ─────────────────────────────────────────────────────────────

    static void SetupAboutPanel()
    {
        GameObject panel = FindInactive("AboutPanel");
        if (panel == null) { Debug.LogError("AboutPanel not found!"); return; }

        ClearChildren(panel);
        AddVerticalLayout(panel, TextAnchor.MiddleCenter);

        CreateLabel(panel, "ABOUT", 48);
        CreateLabel(panel,
            "Parkour Simulator is a first-person\nparkour game built for CSE 457.\n\nWall run, vault, slide, and roll\nthrough challenging urban environments.",
            24);

        GameObject back = CreateButton(panel, "Back", 60);
        MenuManager uiManager = FindMenuManager();
        if (uiManager != null)
            back.GetComponent<Button>().onClick.AddListener(() => uiManager.GoBack());

        EditorUtility.SetDirty(panel);
    }

    // ── Settings ──────────────────────────────────────────────────────────

    static void SetupSettingsPanel()
    {
        GameObject panel = FindInactive("SettingsPanel");
        if (panel == null) { Debug.LogError("SettingsPanel not found!"); return; }

        ClearChildren(panel);
        AddVerticalLayout(panel, TextAnchor.MiddleCenter);

        var settingsUI = panel.GetComponent<SettingsUI>() ?? panel.AddComponent<SettingsUI>();

        CreateLabel(panel, "SETTINGS", 48);
        CreateLabel(panel, "Mouse Sensitivity", 28);

        // Slider
        GameObject sliderObj = new GameObject("SensitivitySlider");
        sliderObj.transform.SetParent(panel.transform, false);
        var slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0.05f;
        slider.maxValue = 1f;
        slider.value = 0.2f;
        var sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.minHeight = 40;
        sliderLayout.preferredHeight = 40;
        slider.onValueChanged.AddListener(settingsUI.OnSensitivityChanged);
        settingsUI.sensitivitySlider = slider;

        // Value label
        GameObject valObj = CreateLabel(panel, "0.20", 24);
        settingsUI.sensitivityValueText = valObj.GetComponent<TextMeshProUGUI>();

        // Back button
        GameObject back = CreateButton(panel, "Back", 60);
        MenuManager uiManager = FindMenuManager();
        if (uiManager != null)
            back.GetComponent<Button>().onClick.AddListener(() => uiManager.GoBack());

        EditorUtility.SetDirty(panel);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void AddVerticalLayout(GameObject go, TextAnchor alignment)
    {
        var vlg = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = alignment;
        vlg.spacing = 12;
        vlg.padding = new RectOffset(40, 40, 30, 30);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
    }

    static GameObject CreateButton(GameObject parent, string label, float minHeight)
    {
        GameObject go = new GameObject(label.Replace(" ", "") + "Button");
        go.transform.SetParent(parent.transform, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.9f, 0.2f, 0.1f);
        colors.pressedColor     = new Color(0.7f, 0.1f, 0.05f);
        btn.colors = colors;

        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = minHeight;

        GameObject textObj = new GameObject("Text (TMP)");
        textObj.transform.SetParent(go.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        btn.targetGraphic = img;

        return go;
    }

    static GameObject CreateLabel(GameObject parent, string text, float fontSize)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = fontSize * 1.8f;

        return go;
    }

    static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    static MenuManager FindMenuManager()
    {
        return Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
    }

    // Finds GameObjects by name including inactive ones
    static GameObject FindInactive(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == name) return go;
        return null;
    }
}
