using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneSetupTool
{
    // ── Tutorial Checkpoint Setup ─────────────────────────────────────────

    [MenuItem("Tools/Setup Tutorial Checkpoints")]
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
