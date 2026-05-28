using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

// Builds the entire main menu in one pass: rebuilds all 5 panels, attaches
// self-wiring components (MenuButton / LevelButtonUI), styles everything, and
// assigns the MenuManager references. Buttons need NO manual Inspector wiring.
public static class UISetupTool
{
    // ── Palette (Mirror's Edge: dark with red accents) ────────────────────
    static readonly Color Backdrop     = new Color(0.04f, 0.05f, 0.07f, 0.72f);
    static readonly Color BtnNormal    = new Color(0.10f, 0.11f, 0.13f, 0.92f);
    static readonly Color BtnHighlight = new Color(0.90f, 0.16f, 0.10f, 1f);
    static readonly Color BtnPressed   = new Color(0.62f, 0.10f, 0.05f, 1f);
    static readonly Color BtnDisabled  = new Color(0.18f, 0.18f, 0.20f, 0.6f);
    static readonly Color AccentRed    = new Color(0.92f, 0.18f, 0.12f, 1f);

    [MenuItem("Tools/Build Menu (setup + wire + style)")]
    public static void BuildMenu()
    {
        GameObject canvas = FindInactive("MainMenuCanvas");
        if (canvas == null) { Debug.LogError("MainMenuCanvas not found. Open the MainMenu scene first."); return; }

        MenuManager mm = FindMenuManager();
        if (mm == null) { Debug.LogError("MenuManager not found in scene."); return; }

        EnsureBackdrop(canvas);

        GameObject landing = GetPanel(canvas, "LandingPanel");
        GameObject select  = GetPanel(canvas, "LevelSelectPanel");
        GameObject about    = GetPanel(canvas, "AboutPanel");
        GameObject settings = GetPanel(canvas, "SettingsPanel");
        GameObject confirm  = GetPanel(canvas, "LevelConfirmPanel");

        BuildLanding(landing);
        BuildLevelSelect(select);
        BuildAbout(about);
        BuildSettings(settings);
        TextMeshProUGUI confirmName = BuildLevelConfirm(confirm);

        // Assign MenuManager references
        mm.landingPanel = landing;
        mm.levelSelectPanel = select;
        mm.aboutPanel = about;
        mm.settingsPanel = settings;
        mm.levelConfirmPanel = confirm;
        mm.confirmLevelNameText = confirmName;
        EditorUtility.SetDirty(mm);

        // Initial visibility: only landing active
        landing.SetActive(true);
        select.SetActive(false);
        about.SetActive(false);
        settings.SetActive(false);
        confirm.SetActive(false);

        Debug.Log("✓ Menu built, wired, and styled. Hit Play — no manual wiring needed.");
    }

    // ── Panel builders ────────────────────────────────────────────────────

    static void BuildLanding(GameObject panel)
    {
        ClearChildren(panel);
        SetupPanel(panel);
        CreateLabel(panel, "PARKOUR", 96, FontStyles.Bold, Color.white, 12);
        CreateNavButton(panel, "Play",     MenuButton.Action.Play);
        CreateNavButton(panel, "About",    MenuButton.Action.About);
        CreateNavButton(panel, "Settings", MenuButton.Action.Settings);
        CreateNavButton(panel, "Exit",     MenuButton.Action.Exit);
        EditorUtility.SetDirty(panel);
    }

    static void BuildLevelSelect(GameObject panel)
    {
        ClearChildren(panel);
        SetupPanel(panel);
        CreateLabel(panel, "SELECT LEVEL", 56, FontStyles.Bold, AccentRed, 6);

        for (int i = 0; i < 3; i++)
        {
            GameObject btn = CreateButton(panel, "Level " + (i + 1), 80);
            var levelBtn = btn.AddComponent<LevelButtonUI>();
            levelBtn.levelIndex = i;
            levelBtn.button = btn.GetComponent<Button>();
            levelBtn.levelNameText = btn.GetComponentInChildren<TextMeshProUGUI>();
        }

        CreateNavButton(panel, "Back", MenuButton.Action.Back, 60);
        EditorUtility.SetDirty(panel);
    }

    static void BuildAbout(GameObject panel)
    {
        ClearChildren(panel);
        SetupPanel(panel);
        CreateLabel(panel, "ABOUT", 56, FontStyles.Bold, AccentRed, 6);
        CreateLabel(panel,
            "Parkour Simulator is a first-person parkour\ngame built for CSE 457.\n\nWall run, vault, slide, and roll through\nchallenging environments.",
            26, FontStyles.Normal, Color.white, 0);
        CreateNavButton(panel, "Back", MenuButton.Action.Back, 60);
        EditorUtility.SetDirty(panel);
    }

    static void BuildSettings(GameObject panel)
    {
        ClearChildren(panel);
        SetupPanel(panel);
        var settingsUI = panel.GetComponent<SettingsUI>() ?? panel.AddComponent<SettingsUI>();

        CreateLabel(panel, "SETTINGS", 56, FontStyles.Bold, AccentRed, 6);
        CreateLabel(panel, "Mouse Sensitivity", 30, FontStyles.Normal, Color.white, 0);

        GameObject sliderObj = new GameObject("SensitivitySlider", typeof(RectTransform));
        sliderObj.transform.SetParent(panel.transform, false);
        var slider = BuildSlider(sliderObj);
        slider.minValue = 0.05f;
        slider.maxValue = 1f;
        slider.value = 0.2f;
        var sliderLayout = sliderObj.AddComponent<LayoutElement>();
        sliderLayout.minHeight = 40;
        sliderLayout.preferredHeight = 40;
        slider.onValueChanged.AddListener(settingsUI.OnSensitivityChanged);
        settingsUI.sensitivitySlider = slider;

        GameObject valObj = CreateLabel(panel, "0.20", 26, FontStyles.Normal, Color.white, 0);
        settingsUI.sensitivityValueText = valObj.GetComponent<TextMeshProUGUI>();

        CreateNavButton(panel, "Back", MenuButton.Action.Back, 60);
        EditorUtility.SetDirty(panel);
    }

    static TextMeshProUGUI BuildLevelConfirm(GameObject panel)
    {
        ClearChildren(panel);
        SetupPanel(panel);
        CreateLabel(panel, "START LEVEL?", 56, FontStyles.Bold, AccentRed, 6);

        GameObject nameObj = CreateLabel(panel, "Level Name", 34, FontStyles.Bold, Color.white, 0);
        nameObj.name = "LevelTextName";

        CreateNavButton(panel, "Start", MenuButton.Action.StartLevel);
        CreateNavButton(panel, "Back",  MenuButton.Action.CancelConfirm, 60);

        EditorUtility.SetDirty(panel);
        return nameObj.GetComponent<TextMeshProUGUI>();
    }

    // ── Element builders ──────────────────────────────────────────────────

    static void CreateNavButton(GameObject parent, string label, MenuButton.Action action, float minHeight = 70)
    {
        GameObject btn = CreateButton(parent, label, minHeight);
        var mb = btn.AddComponent<MenuButton>();
        mb.action = action;
    }

    static GameObject CreateButton(GameObject parent, string label, float minHeight)
    {
        GameObject go = new GameObject(label.Replace(" ", "") + "Button", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);

        var img = go.AddComponent<Image>();
        img.color = Color.white;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor      = BtnNormal;
        colors.highlightedColor = BtnHighlight;
        colors.pressedColor     = BtnPressed;
        colors.selectedColor    = BtnNormal;
        colors.disabledColor    = BtnDisabled;
        colors.fadeDuration     = 0.12f;
        btn.colors = colors;

        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = minHeight;
        layout.preferredHeight = minHeight;

        GameObject textObj = new GameObject("Text (TMP)", typeof(RectTransform));
        textObj.transform.SetParent(go.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 34;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        Stretch(textObj.GetComponent<RectTransform>());

        return go;
    }

    static GameObject CreateLabel(GameObject parent, string text, float fontSize, FontStyles style, Color color, float spacing)
    {
        GameObject go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.characterSpacing = spacing;

        var layout = go.AddComponent<LayoutElement>();
        layout.minHeight = fontSize * 1.8f;

        return go;
    }

    static Slider BuildSlider(GameObject go)
    {
        var slider = go.AddComponent<Slider>();

        GameObject bg = new GameObject("Background", typeof(RectTransform));
        bg.transform.SetParent(go.transform, false);
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.22f, 1f);
        Stretch(bg.GetComponent<RectTransform>());

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = AccentRed;
        Stretch(fill.GetComponent<RectTransform>());

        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 0);

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;

        return slider;
    }

    // ── Styling / layout ──────────────────────────────────────────────────

    static void EnsureBackdrop(GameObject canvas)
    {
        Transform existing = canvas.transform.Find("Backdrop");
        GameObject backdrop = existing != null ? existing.gameObject
            : new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        if (existing == null) backdrop.transform.SetParent(canvas.transform, false);
        backdrop.transform.SetSiblingIndex(0);
        var img = backdrop.GetComponent<Image>();
        img.color = Backdrop;
        img.raycastTarget = false;
        Stretch(backdrop.GetComponent<RectTransform>());
    }

    static void SetupPanel(GameObject panel)
    {
        var img = panel.GetComponent<Image>();
        if (img != null) img.color = new Color(0, 0, 0, 0); // transparent; backdrop shows
        Stretch(panel.GetComponent<RectTransform>());

        var vlg = panel.GetComponent<VerticalLayoutGroup>() ?? panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 16;
        vlg.padding = new RectOffset(80, 80, 60, 60);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject GetPanel(GameObject canvas, string name)
    {
        Transform t = canvas.transform.Find(name);
        if (t != null) return t.gameObject;

        // Create the panel if it doesn't exist
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        return panel;
    }

    static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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

    static GameObject FindInactive(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == name) return go;
        return null;
    }
}
