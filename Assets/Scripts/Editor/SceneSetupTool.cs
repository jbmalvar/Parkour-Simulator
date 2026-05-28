using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public static class SceneSetupTool
{
    // ── Main Menu Scene Setup ─────────────────────────────────────────────

    [MenuItem("Tools/1 - Create MainMenu Scene")]
    public static void CreateMainMenuScene()
    {
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";

        // Save current scene first
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Open MovementScene to grab UI objects
        Scene movementScene = EditorSceneManager.OpenScene("Assets/Scenes/MovementScene.unity", OpenSceneMode.Single);

        // Find UI objects to move
        string[] objectsToMove = { "MainMenuCanvas", "ScreenFade Canvas", "EventSystem" };
        List<GameObject> found = new List<GameObject>();

        foreach (var root in movementScene.GetRootGameObjects())
        {
            foreach (var name in objectsToMove)
                if (root.name == name) { found.Add(root); break; }
        }

        if (found.Count == 0)
        {
            Debug.LogError("Could not find MainMenuCanvas, ScreenFade Canvas or EventSystem in MovementScene. Make sure you're running this from MovementScene.");
            return;
        }

        // Create the MainMenu scene
        Scene mainMenu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        mainMenu.name = "MainMenu";

        // Move UI objects into MainMenu scene
        foreach (var go in found)
            SceneManager.MoveGameObjectToScene(go, mainMenu);

        // Add a directional light to MainMenu so it's not pitch black
        var lightGO = new GameObject("Directional Light");
        SceneManager.MoveGameObjectToScene(lightGO, mainMenu);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        // Save MainMenu scene
        EditorSceneManager.SaveScene(mainMenu, mainMenuPath);

        // Save MovementScene (now without the UI objects)
        EditorSceneManager.SaveScene(movementScene);

        // Update Build Settings
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(mainMenuPath, true),
            new EditorBuildSettingsScene("Assets/Scenes/Tutorial (Index 0).unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MovementScene.unity", true),
        };
        EditorBuildSettings.scenes = scenes;

        Debug.Log("✓ MainMenu scene created at " + mainMenuPath + " and build settings updated!");
        Debug.Log("  Open MainMenu.unity and hit Play to test the menu.");
    }

    // ── Tutorial Checkpoint Setup ─────────────────────────────────────────

    [MenuItem("Tools/2 - Setup Tutorial Checkpoint")]
    public static void SetupTutorialCheckpoint()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        Scene tutorial = EditorSceneManager.OpenScene("Assets/Scenes/Tutorial (Index 0).unity", OpenSceneMode.Single);

        // --- CheckpointManager ---
        GameObject mgr = new GameObject("CheckpointManager");
        SceneManager.MoveGameObjectToScene(mgr, tutorial);
        var cm = mgr.AddComponent<CheckpointManager>();
        cm.deathYThreshold = -20f;
        cm.respawnDelay = 0.4f;

        // Find player in scene to assign
        foreach (var root in tutorial.GetRootGameObjects())
        {
            if (root.name == "Player")
            {
                cm.player = root.transform;
                break;
            }
        }

        // --- ScreenFade Canvas ---
        GameObject fadeCanvas = new GameObject("ScreenFade Canvas");
        SceneManager.MoveGameObjectToScene(fadeCanvas, tutorial);
        var canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        fadeCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        fadeCanvas.AddComponent<GraphicRaycaster>();

        GameObject fadeImg = new GameObject("FadeImage");
        fadeImg.transform.SetParent(fadeCanvas.transform, false);
        var img = fadeImg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);
        var rt = fadeImg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var fade = fadeCanvas.AddComponent<ScreenFade>();
        fade.fadeImage = img;
        cm.screenFade = fade;

        // --- Checkpoint 1 (near the start) ---
        CreateCheckpoint(tutorial, "Checkpoint_1", new Vector3(0, 1f, 5f));

        // --- Death Zone (below the map) ---
        GameObject deathZone = new GameObject("DeathZone");
        SceneManager.MoveGameObjectToScene(deathZone, tutorial);
        deathZone.transform.position = new Vector3(0, -25f, 0);
        deathZone.transform.localScale = new Vector3(500f, 1f, 500f);
        var dzCol = deathZone.AddComponent<BoxCollider>();
        dzCol.isTrigger = true;
        deathZone.AddComponent<DeathZone>();

        EditorSceneManager.SaveScene(tutorial);
        Debug.Log("✓ Tutorial checkpoint setup complete! Adjust Checkpoint_1 position in the scene to fit your level.");
    }

    static void CreateCheckpoint(Scene scene, string name, Vector3 position)
    {
        // Beacon — tall thin cylinder
        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = name;
        SceneManager.MoveGameObjectToScene(beacon, scene);
        beacon.transform.position = position;
        beacon.transform.localScale = new Vector3(0.15f, 2f, 0.15f);

        // Red emissive material
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.15f, 0.05f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.05f) * 2f);
        beacon.GetComponent<Renderer>().material = mat;

        // Trigger collider (wider than the beacon so it's easy to walk through)
        var col = beacon.GetComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 1.5f;

        var cp = beacon.AddComponent<Checkpoint>();
        cp.beamRenderer = beacon.GetComponent<Renderer>();
        cp.spawnOffset = new Vector3(0, 0.1f, 0);
    }
}
