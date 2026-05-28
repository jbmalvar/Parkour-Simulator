using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

// Non-destructive menu styling. Only edits components on existing objects,
// so manually-wired Button OnClick events are preserved.
public static class MenuPolishTool
{
    // ── Mirror's Edge inspired palette ───────────────────────────────────
    static readonly Color Backdrop      = new Color(0.04f, 0.05f, 0.07f, 0.72f);
    static readonly Color PanelTint     = new Color(0f, 0f, 0f, 0f);     // transparent (backdrop shows)
    static readonly Color BtnNormal     = new Color(0.10f, 0.11f, 0.13f, 0.92f);
    static readonly Color BtnHighlight  = new Color(0.90f, 0.16f, 0.10f, 1f);
    static readonly Color BtnPressed    = new Color(0.62f, 0.10f, 0.05f, 1f);
    static readonly Color BtnDisabled   = new Color(0.18f, 0.18f, 0.20f, 0.6f);
    static readonly Color TextColor     = Color.white;
    static readonly Color AccentRed     = new Color(0.92f, 0.18f, 0.12f, 1f);

    [MenuItem("Tools/3 - Polish Menu Style")]
    public static void PolishMenu()
    {
        GameObject canvas = FindInactive("MainMenuCanvas");
        if (canvas == null) { Debug.LogError("MainMenuCanvas not found. Open the MainMenu scene first."); return; }

        EnsureBackdrop(canvas);
        StylePanels(canvas);
        StyleButtons(canvas);
        StyleTexts(canvas);

        EditorUtility.SetDirty(canvas);
        Debug.Log("✓ Menu styled. (Button wiring preserved.)");
    }

    // A single dark full-screen image behind every panel
    static void EnsureBackdrop(GameObject canvas)
    {
        Transform existing = canvas.transform.Find("Backdrop");
        GameObject backdrop;
        if (existing != null) backdrop = existing.gameObject;
        else
        {
            backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(canvas.transform, false);
            backdrop.AddComponent<Image>();
        }

        backdrop.transform.SetSiblingIndex(0); // render behind all panels
        var img = backdrop.GetComponent<Image>();
        img.color = Backdrop;
        img.raycastTarget = false;
        Stretch(backdrop.GetComponent<RectTransform>());
    }

    static void StylePanels(GameObject canvas)
    {
        string[] panelNames = { "LandingPanel", "LevelSelectPanel", "AboutPanel", "SettingsPanel", "LevelConfirmPanel" };
        foreach (var name in panelNames)
        {
            Transform p = canvas.transform.Find(name);
            if (p == null) continue;

            var img = p.GetComponent<Image>();
            if (img != null) img.color = PanelTint;

            Stretch(p.GetComponent<RectTransform>());

            // Tidy the vertical layout if present
            var vlg = p.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 16;
                vlg.padding = new RectOffset(60, 60, 50, 50);
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }
        }
    }

    static void StyleButtons(GameObject canvas)
    {
        foreach (var btn in canvas.GetComponentsInChildren<Button>(true))
        {
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.white; // tint multiplies against state colors
                btn.targetGraphic = img;
            }

            btn.transition = Selectable.Transition.ColorTint;
            var c = btn.colors;
            c.normalColor      = BtnNormal;
            c.highlightedColor = BtnHighlight;
            c.pressedColor     = BtnPressed;
            c.selectedColor    = BtnNormal;
            c.disabledColor    = BtnDisabled;
            c.fadeDuration     = 0.12f;
            btn.colors = c;

            // Constrain button height
            var le = btn.GetComponent<LayoutElement>() ?? btn.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 70;
            le.preferredHeight = 70;

            EditorUtility.SetDirty(btn);
        }
    }

    static void StyleTexts(GameObject canvas)
    {
        foreach (var tmp in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            bool isTitle = tmp.gameObject.name.ToUpper().Contains("TITLE");
            bool isButtonLabel = tmp.GetComponentInParent<Button>() != null;

            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;

            if (isTitle)
            {
                tmp.fontSize = 96;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = TextColor;
                tmp.characterSpacing = 12;
            }
            else if (isButtonLabel)
            {
                tmp.fontSize = 34;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = TextColor;
            }
            else
            {
                // Panel headers / body text
                bool looksLikeHeader = tmp.text == tmp.text.ToUpper() && tmp.text.Length < 24;
                tmp.fontSize = looksLikeHeader ? 56 : 26;
                tmp.fontStyle = looksLikeHeader ? FontStyles.Bold : FontStyles.Normal;
                tmp.color = looksLikeHeader ? AccentRed : TextColor;
            }

            EditorUtility.SetDirty(tmp);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static GameObject FindInactive(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == name) return go;
        return null;
    }
}
