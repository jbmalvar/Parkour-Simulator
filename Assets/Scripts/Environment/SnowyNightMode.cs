using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Re-applies the dark, foggy, cyan-neon "night" look at runtime, every time the scene
/// loads. Because it runs in code on Start (Play and builds), it survives teammates
/// overwriting the binary scene file — the look can't get "lost on push" anymore.
///
/// Drop ONE of these in the scene (Tools ▸ Snowy Wonderland ▸ Add Persistent Night Mode).
/// It darkens the environment, kills stray lights, removes the platforms' self-glow,
/// plants a cyan lamp on every checkpoint, and sets up bloom/vignette so the lamps glow.
/// </summary>
public class SnowyNightMode : MonoBehaviour
{
    [Header("Darkness")]
    public Color ambient = new Color(0.002f, 0.004f, 0.002f);
    public float directionalIntensity = 0.012f;
    public Color fogColor = new Color(0.01f, 0.025f, 0.035f);
    public float fogDensity = 0.07f;

    [Header("Cyan neon lamps")]
    public Color lampColor = new Color(0.30f, 0.85f, 1f);
    public float lampRange = 3.5f;       // super low — barely lights anything
    public float lampIntensity = 4f;

    [Header("Bloom (lamp glow)")]
    public float bloomIntensity = 1.8f;
    public float bloomThreshold = 0.6f;

    const string LampName = "CheckpointLamp";

    // Start (not Awake) so checkpoints, cameras and the course are all present.
    void Start() => Apply();

    public void Apply()
    {
        ApplyEnvironment();
        DimStrayLights();
        StripCourseGlow();
        PlaceLamps();
        TrySetupPostFx();
    }

    void ApplyEnvironment()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        RenderSettings.reflectionIntensity = 0f;     // no bright sky mirrored in the ice
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
    }

    void DimStrayLights()
    {
        foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (IsLamp(l.transform)) continue;        // keep our checkpoint lamps
            if (l.type == LightType.Directional)
            {
                l.intensity = directionalIntensity;
                l.color = new Color(0.6f, 0.7f, 0.95f);
            }
            else
            {
                l.intensity = 0f;                     // kill stray point/spot lights
            }
        }
    }

    // The platform ice/snow materials carry a baked emission so they glow on their own;
    // strip it so the world is genuinely dark (runtime-only edit — never written to disk).
    void StripCourseGlow()
    {
        var root = GameObject.Find("FrozenGauntlet");
        if (root == null) return;

        var done = new HashSet<Material>();
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<Checkpoint>() != null || r.name == "LampHead" || r.name == "FinishPortal")
                continue;                             // keep beacons / lamp heads / portal glowing
            var m = r.sharedMaterial;
            if (m == null || !done.Add(m)) continue;
            if (m.HasProperty("_EmissionColor"))
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    void PlaceLamps()
    {
        foreach (var go in FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.name == LampName) Destroy(go);

        var headMat = UnlitGlow(lampColor, 4f);
        var postMat = new Material(LitShader()) { color = new Color(0.05f, 0.05f, 0.06f) };

        foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
            CreateLamp(cp.transform, headMat, postMat);
    }

    void CreateLamp(Transform checkpoint, Material headMat, Material postMat)
    {
        Vector3 p = checkpoint.position;
        float platformTop = p.y - 1f;                 // beacon centre sits ~1m above its platform

        var lamp = new GameObject(LampName);
        lamp.transform.SetParent(checkpoint.parent, worldPositionStays: true);
        lamp.transform.position = new Vector3(p.x, platformTop, p.z);
        lamp.transform.rotation = Quaternion.identity;

        MakePart(lamp.transform, PrimitiveType.Cylinder, "Post",
            new Vector3(0, 1.6f, 0), new Vector3(0.12f, 1.6f, 0.12f), Quaternion.identity, postMat);
        MakePart(lamp.transform, PrimitiveType.Cylinder, "Arm",
            new Vector3(0.25f, 3.2f, 0), new Vector3(0.08f, 0.35f, 0.08f), Quaternion.Euler(0, 0, 90), postMat);
        MakePart(lamp.transform, PrimitiveType.Sphere, "LampHead",
            new Vector3(0.5f, 3.15f, 0), Vector3.one * 0.7f, Quaternion.identity, headMat);

        var lightGo = new GameObject("LampLight");
        lightGo.transform.SetParent(lamp.transform, false);
        lightGo.transform.localPosition = new Vector3(0.5f, 3.15f, 0);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lampColor;
        light.range = lampRange;
        light.intensity = lampIntensity;
        light.shadows = LightShadows.None;            // perf: many lamps in a level
    }

    static void MakePart(Transform parent, PrimitiveType type, string name, Vector3 localPos,
                         Vector3 localScale, Quaternion localRot, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localRotation = localRot;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    void TrySetupPostFx()
    {
        // Guarded: if bloom setup ever fails it must not break the rest of the look.
        try
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(bloomThreshold);
            bloom.intensity.Override(bloomIntensity);
            bloom.scatter.Override(0.8f);
            bloom.tint.Override(new Color(0.7f, 0.92f, 1f));

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.45f);
            vig.smoothness.Override(0.45f);
            vig.color.Override(Color.black);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(-0.2f);
            grade.contrast.Override(20f);
            grade.colorFilter.Override(new Color(0.82f, 0.9f, 1f));
            grade.saturation.Override(-5f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            var volGo = GameObject.Find("Night Volume") ?? new GameObject("Night Volume");
            var vol = volGo.GetComponent<Volume>() ?? volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10f;
            vol.sharedProfile = profile;

            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SnowyNightMode: post-processing setup skipped ({e.Message}).");
        }
    }

    // ── helpers ───────────────────────────────────────────────────────────

    bool IsLamp(Transform t)
    {
        for (var cur = t; cur != null; cur = cur.parent)
            if (cur.name == LampName) return true;
        return false;
    }

    static Shader LitShader() =>
        Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    static Material UnlitGlow(Color c, float intensity)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? LitShader();
        var m = new Material(shader);
        Color hdr = c * intensity;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        m.color = hdr;
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", hdr);
        }
        return m;
    }
}
