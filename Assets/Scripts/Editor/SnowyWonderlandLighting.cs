using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes SnowyWonderland a dark, barely-visible night and plants a glowing lamp post
/// at every checkpoint so the lamps become the guide-lights along the route.
///
/// Standalone: it only changes the scene's lighting settings and adds "CheckpointLamp"
/// objects next to existing Checkpoints. It does NOT rebuild the course or touch the
/// player/UI. Re-running is safe (it clears old lamps first). Tune the constants below
/// or tweak the created lights/RenderSettings afterwards to taste.
/// </summary>
public static class SnowyWonderlandLighting
{
    const string LampName = "CheckpointLamp";

    // --- Darkness / visibility knobs (raise to see more, lower to see less) ---
    static readonly Color AmbientColor = new Color(0.002f, 0.004f, 0.002f); // borderline unseeable
    const float DirectionalIntensity = 0.012f;                             // almost no moonlight
    const float FogDensity = 0.07f;        // higher = thicker haze = you see LESS distance

    // --- Lamp knobs (cyan neon, to match the reference) ---
    // The glowing HEAD (emissive + bloom) is what marks each checkpoint; the cast light
    // is deliberately tiny so it barely illuminates anything around it.
    static readonly Color LampColor = new Color(0.30f, 0.85f, 1f);
    const float LampRange = 3.5f;      // super low — a faint pool right at the lamp
    const float LampIntensity = 4f;

    // --- Death-plane knobs (local to the FrozenGauntlet root) ---
    const float DeathPlaneTop = 2.5f;  // was 1.5 and thin; raise + thicken so falls die fast.
    const float DeathPlaneThickness = 28f;

    [MenuItem("Tools/Snowy Wonderland/Dark Mode + Checkpoint Lamps")]
    public static void Apply()
    {
        Darken();
        int n = PlaceLamps();
        int mats = KillPlatformGlow();   // <-- the real reason it stayed bright
        SetupPostFx();                   // bloom/vignette/grade so the neon glows
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        if (n == 0)
            Debug.LogWarning("No Checkpoints found in the scene, so no lamps were placed. " +
                             "Lamps are placed on Checkpoint objects — run Build Frozen Gauntlet " +
                             "(or make sure the course/checkpoints are present), then run this again.");
        Debug.Log($"🌙 Dark neon look applied: {n} lamp(s), glow stripped from {mats} platform material(s), " +
                  "bloom/vignette enabled. If it's now TOO dark, raise DirectionalIntensity/AmbientColor " +
                  "(or a lamp's Range/Intensity).");
    }

    [MenuItem("Tools/Snowy Wonderland/Raise Death Plane")]
    public static void RaiseDeathPlane()
    {
        // The kill-volume the builder makes is named "CrevasseFloor" and lives under the
        // FrozenGauntlet root. Raise its top and make it thick so a fast fall can't tunnel
        // straight through (which made death feel slow / "too low").
        GameObject floor = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name == "CrevasseFloor") { floor = go; break; }

        if (floor == null)
        {
            Debug.LogWarning("Raise Death Plane: no 'CrevasseFloor' found. Re-run Build Frozen Gauntlet first.");
            return;
        }

        var t = floor.transform;
        var lp = t.localPosition;
        var ls = t.localScale;
        t.localPosition = new Vector3(lp.x, DeathPlaneTop - DeathPlaneThickness / 2f, lp.z);
        t.localScale = new Vector3(ls.x, DeathPlaneThickness, ls.z);

        // Make sure it's still a trigger that kills.
        var col = floor.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Death plane raised to local y={DeathPlaneTop} and thickened to {DeathPlaneThickness}. " +
                  "Lower DeathPlaneTop if you want to fall further before dying.");
    }

    [MenuItem("Tools/Snowy Wonderland/Remove Checkpoint Lamps")]
    public static void RemoveLamps()
    {
        int n = 0;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name == LampName) { Object.DestroyImmediate(go); n++; }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Removed {n} checkpoint lamp(s).");
    }

    // ──────────────────────────────────────────────────────────────────────

    static void Darken()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = AmbientColor;

        // Atmospheric haze for depth, so the neon fades into the dark like the reference.
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.01f, 0.025f, 0.035f);  // very dark teal
        RenderSettings.fogDensity = FogDensity;

        // Glossy ice otherwise mirrors the bright sky and looks lit even with no ambient.
        // Kill skybox reflections, and swap in a dark night sky so the backdrop is dark too.
        RenderSettings.reflectionIntensity = 0f;
        var nightSky = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/UnityAssets/Skybox/Cubemaps/Classic/FS000_Night_01_Moonless.mat");
        if (nightSky != null) RenderSettings.skybox = nightSky;

        // The real fix for "still too bright": dim/kill EVERY light that isn't one of our
        // checkpoint lamps — directionals AND any stray point/spot lights from the original
        // level, the player rig, or decorations (previously only directionals were handled).
        int dir = 0, killed = 0;
        foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (IsLampLight(l.transform)) continue;   // keep the checkpoint lamps lit

            Debug.Log($"[DarkMode] found light '{l.name}'  type={l.type}  intensity={l.intensity}  -> dimming");
            if (l.type == LightType.Directional)
            {
                l.intensity = DirectionalIntensity;
                l.color = new Color(0.6f, 0.7f, 0.95f);
                dir++;
            }
            else
            {
                l.intensity = 0f;     // kill stray point/spot lights that were flooding the scene
                killed++;
            }
        }
        Debug.Log($"[DarkMode] dimmed {dir} directional light(s) and killed {killed} point/spot light(s). " +
                  "Only the checkpoint lamps remain. (Check the lines above to see what was lighting it up.)");
    }

    // True if this light belongs to one of our CheckpointLamp objects.
    static bool IsLampLight(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
            if (cur.name == LampName) return true;
        return false;
    }

    /// <summary>
    /// The platform ice/snow materials were given a faint emission earlier so they'd be
    /// visible — but that self-glow ignores scene lighting, which is why the level stays
    /// bright no matter how low ambient goes. Strip emission from the course's own
    /// (scene-embedded) materials, while LEAVING the checkpoint beacons, lamp heads and
    /// finish portal glowing. Prefab/asset materials are skipped so we never edit an asset.
    /// </summary>
    static int KillPlatformGlow()
    {
        var root = GameObject.Find("FrozenGauntlet");
        if (root == null) return 0;

        var keep = new System.Collections.Generic.HashSet<Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            bool glowy = r.GetComponentInParent<Checkpoint>() != null
                      || r.name == "LampHead"
                      || r.name == "FinishPortal";
            if (glowy && r.sharedMaterial != null) keep.Add(r.sharedMaterial);
        }

        var done = new System.Collections.Generic.HashSet<Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var m = r.sharedMaterial;
            if (m == null || keep.Contains(m) || AssetDatabase.Contains(m)) continue; // skip glowy + prefab assets
            if (!done.Add(m)) continue;
            m.DisableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }
        return done.Count;
    }

    /// <summary>
    /// Builds the moody "neon in the dark" post-processing: a global Volume with Bloom
    /// (makes the cyan lamps glow), Vignette (dark edges), a cool colour grade and
    /// tonemapping. Reuses the project's SnowyWonderland_PostProcess profile and makes
    /// sure the cameras actually run post-processing (otherwise none of it shows).
    /// </summary>
    static void SetupPostFx()
    {
        const string profilePath = "Assets/Settings/SnowyWonderland_PostProcess.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            Debug.LogWarning($"Post-process: profile not found at {profilePath}; skipping bloom.");
            return;
        }

        var bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.6f);
        bloom.intensity.Override(1.8f);
        bloom.scatter.Override(0.8f);
        bloom.tint.Override(new Color(0.7f, 0.92f, 1f));

        var vig = GetOrAdd<Vignette>(profile);
        vig.active = true;
        vig.intensity.Override(0.45f);
        vig.smoothness.Override(0.45f);
        vig.color.Override(Color.black);

        var grade = GetOrAdd<ColorAdjustments>(profile);
        grade.active = true;
        grade.postExposure.Override(-0.2f);
        grade.contrast.Override(20f);
        grade.colorFilter.Override(new Color(0.82f, 0.9f, 1f));
        grade.saturation.Override(-5f);

        var tone = GetOrAdd<Tonemapping>(profile);
        tone.active = true;
        tone.mode.Override(TonemappingMode.Neutral);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Global volume that applies the profile everywhere.
        var volGo = GameObject.Find("Global Light Volume") ?? new GameObject("Global Light Volume");
        var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 10f;
        vol.sharedProfile = profile;

        // Post-processing must be enabled per-camera or the Volume does nothing.
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    static T GetOrAdd<T>(VolumeProfile p) where T : VolumeComponent
    {
        if (p.TryGet<T>(out var existing)) return existing;
        var c = p.Add<T>(false);
        AssetDatabase.AddObjectToAsset(c, p);
        return c;
    }

    static int PlaceLamps()
    {
        // Clear previous lamps so re-running doesn't stack them.
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name == LampName) Object.DestroyImmediate(go);

        var headMat = Emissive(LampColor, 6f);   // strong so it blooms
        var postMat = Matte(new Color(0.05f, 0.05f, 0.06f));

        int n = 0;
        foreach (var cp in Object.FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            CreateLamp(cp.transform, headMat, postMat);
            n++;
        }
        return n;
    }

    static void CreateLamp(Transform checkpoint, Material headMat, Material postMat)
    {
        // The checkpoint beacon's centre sits ~1m above its platform, and the beacon has
        // a squashed scale — so anchor the lamp in world space under the checkpoint's
        // PARENT (not the beacon) to avoid inheriting that scale.
        Vector3 p = checkpoint.position;
        float platformTop = p.y - 1f;

        var lamp = new GameObject(LampName);
        lamp.transform.SetParent(checkpoint.parent, worldPositionStays: true);
        lamp.transform.position = new Vector3(p.x, platformTop, p.z);
        lamp.transform.rotation = Quaternion.identity;

        // Post.
        var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Post";
        post.transform.SetParent(lamp.transform, false);
        post.transform.localScale = new Vector3(0.12f, 1.6f, 0.12f);
        post.transform.localPosition = new Vector3(0, 1.6f, 0);     // 0 → 3.2m tall
        post.GetComponent<Renderer>().sharedMaterial = postMat;
        Object.DestroyImmediate(post.GetComponent<Collider>());

        // A little cross-arm so it reads as a lamp post.
        var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arm.name = "Arm";
        arm.transform.SetParent(lamp.transform, false);
        arm.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
        arm.transform.localPosition = new Vector3(0.25f, 3.2f, 0);
        arm.transform.localRotation = Quaternion.Euler(0, 0, 90);
        arm.GetComponent<Renderer>().sharedMaterial = postMat;
        Object.DestroyImmediate(arm.GetComponent<Collider>());

        // Glowing lamp head.
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "LampHead";
        head.transform.SetParent(lamp.transform, false);
        head.transform.localScale = Vector3.one * 0.5f;
        head.transform.localPosition = new Vector3(0.5f, 3.15f, 0);
        head.GetComponent<Renderer>().sharedMaterial = headMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());

        // The actual light.
        var lightGo = new GameObject("LampLight");
        lightGo.transform.SetParent(lamp.transform, false);
        lightGo.transform.localPosition = new Vector3(0.5f, 3.15f, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = LampColor;
        light.range = LampRange;
        light.intensity = LampIntensity;
        light.shadows = LightShadows.Soft;     // set to None if you hit a perf dip
    }

    // ── material helpers ──────────────────────────────────────────────────

    static Material Matte(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        return new Material(shader) { color = c };
    }

    static Material Emissive(Color c, float strength)
    {
        var m = Matte(c);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * strength);
        return m;
    }
}
