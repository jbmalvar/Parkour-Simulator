using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click builder for the "Frozen Gauntlet" parkour course in SnowyWonderland.
///
/// SnowyWonderland.unity is binary-serialized, so it can't be hand-edited as text.
/// Instead, open the scene and run Tools ▸ Snowy Wonderland ▸ Build Frozen Gauntlet.
/// Everything is parented under a single "FrozenGauntlet" root so it is trivial to
/// move, rotate, undo, or clear. Re-running rebuilds from scratch.
///
/// The course is an elevated ice-shelf built along +Z. The original flat ground
/// becomes a deadly crevasse: any fall lands in a Hazard volume that respawns you
/// at the last checkpoint. The player is teleported onto the start ledge.
/// </summary>
public static class SnowyWonderlandBuilder
{
    const string RootName = "FrozenGauntlet";
    const int WallsLayer = 6;          // matches the player's wallLayer / vaultLayer (m_Bits 64)

    // Course heights (local to the root)
    const float Top = 5f;              // surface height of the ice shelf above the player's spawn
    const float KillTopY = 2.5f;       // TOP of the crevasse kill-volume — below the lowest
                                       // walkable surface (avalanche bottom = Top-2 = 3) but high
                                       // enough that falls die quickly. The volume is THICK so a
                                       // fast fall can't tunnel through it.
    const float KillThickness = 28f;
    const float Thick = 0.5f;          // platform slab thickness

    // Shared materials (built once per Build)
    static Material _ice, _packedSnow, _deepIce, _hazard, _wall, _checkpointMat, _portalMat;

    // ──────────────────────────────────────────────────────────────────────
    //  Menu entries
    // ──────────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Snowy Wonderland/Build Frozen Gauntlet")]
    public static void Build()
    {
        BuildMaterials();

        // Wipe any previous build so the tool is idempotent.
        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Frozen Gauntlet");
        var r = root.transform;

        // Place the course where the player currently is, then drop the player onto the start.
        Transform player = FindPlayer();
        if (player != null)
        {
            r.position = new Vector3(player.position.x, player.position.y, player.position.z);

            // Hazard / MovingPlatform / CrushingBoulder use strict CompareTag("Player"),
            // so guarantee the player carries that tag or those interactions silently fail.
            if (!player.CompareTag("Player"))
            {
                player.tag = "Player";
                Debug.Log("Tagged the player 'Player' so hazards and platforms detect it.");
            }
        }
        else
        {
            Debug.LogWarning("No player found (PlayerMovement / 'Player' tag). " +
                             "The course will still build, but place a player at the start ledge yourself.");
        }

        EnsureCheckpointManager(player);
        DisableYeti();

        // Build the segments along +Z, threading a running z-cursor through each.
        float z = 0f;
        z = BuildStartLedge(r, z);
        z = BuildFrozenGap(r, z);
        z = BuildGlacierCanyon(r, z);
        z = BuildBrittleBridge(r, z);
        z = BuildAvalancheChute(r, z);
        z = BuildFloeCrossing(r, z);
        // Act 2 — second half of the run.
        z = BuildDashGauntlet(r, z);
        z = BuildZigzagWallRun(r, z);
        z = BuildFrozenSteps(r, z);
        z = BuildSerpentBridge(r, z);
        z = BuildSummit(r, z);

        // One continuous crevasse floor under the WHOLE course so any fall = respawn.
        BuildKillFloor(r, z);

        // Drop the player onto the start ledge, facing down the course (+Z).
        if (player != null)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = r.TransformPoint(new Vector3(0f, Top + 0.2f, 1.5f));
            player.rotation = r.rotation;          // root has no rotation, so this faces +Z
            if (cc != null) cc.enabled = true;
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"❄ Frozen Gauntlet built ({z:0}m long). Move/rotate the '{RootName}' root to reposition. " +
                  "Press Play to run it. (Undo or Tools ▸ Clear to remove.)");
    }

    [MenuItem("Tools/Snowy Wonderland/Clear Frozen Gauntlet")]
    public static void Clear()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Frozen Gauntlet removed.");
        }
        else Debug.Log("No Frozen Gauntlet found in the scene.");
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 0 — Start ledge + crevasse hazard
    // ──────────────────────────────────────────────────────────────────────

    static float BuildStartLedge(Transform root, float z)
    {
        var seg = Group(root, "0_StartLedge");
        // A wide, safe launch pad.
        Platform(seg, new Vector3(0, Top, z + 3f), new Vector2(10f, 8f), _packedSnow);
        Beacon(seg,new Vector3(0, Top, z + 2f), "Checkpoint_Start");

        // A snowy backdrop wall so the player doesn't wander backwards off the start.
        Box("StartWall", seg, new Vector3(0, Top + 2f, z - 1f), new Vector3(10f, 4f, 0.5f), _packedSnow);

        // Decorative welcome snowmen flanking the start.
        SpawnPrefab("Assets/Prefabs/Snowman_Stylized.prefab", seg,
            new Vector3(-4.5f, Top, z + 4f), Quaternion.Euler(0, 30, 0), 1.5f);
        SpawnPrefab("Assets/Prefabs/Snowman_Stylized.prefab", seg,
            new Vector3(4.5f, Top, z + 4f), Quaternion.Euler(0, -30, 0), 1.5f);

        return z + 6f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 1 — Frozen Gap: staggered ice floes over the crevasse (JUMP)
    // ──────────────────────────────────────────────────────────────────────

    static float BuildFrozenGap(Transform root, float z)
    {
        var seg = Group(root, "1_FrozenGap");

        // Staggered floating ice floes. Centers ~5.5m apart (≈2.5m edge gaps),
        // alternating left/right so the player has to aim each jump.
        float[] xs   = { -2.5f, 2.5f, -2.5f, 2.0f, -1.5f };
        float[] dys  = {  0f,   0.6f,  0f,   0.8f,  0.3f }; // slight height stagger
        float zc = z + 4.5f;
        foreach (var i in Indices(xs.Length))
        {
            Platform(seg, new Vector3(xs[i], Top + dys[i], zc), new Vector2(3f, 3f), _ice);
            // An icicle planted on a floe for flavour.
            if (i % 2 == 0)
                SpawnPrefab("Assets/Prefabs/Icicle_Stylized.prefab", seg,
                    new Vector3(xs[i], Top + dys[i] + 0.2f, zc), Quaternion.identity, 0.7f);
            zc += 5.5f;
        }
        float end = zc - 5.5f + 3f;

        // Landing ledge + checkpoint.
        Platform(seg, new Vector3(0, Top, end + 2f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg,new Vector3(0, Top, end + 1.5f), "Checkpoint_1");
        return end + 4.5f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 2 — Glacier Canyon: wall-run across a gap too wide to jump
    // ──────────────────────────────────────────────────────────────────────

    static float BuildGlacierCanyon(Transform root, float z)
    {
        var seg = Group(root, "2_GlacierCanyon");
        float runLen = 11f;                 // far too wide for even a dash-jump

        // Tall ice wall on the RIGHT for the player to wall-run along (Walls layer).
        // The approach lane hugs the wall (player at x≈1.5, wall face at x≈2.1, well
        // within the 0.7m wallDetectionRange) so a forward jump off the edge cleanly
        // kicks off a wall-run.
        Box("RunwayLedge", seg, new Vector3(1.5f, Top - Thick / 2f, z + 1.75f),
            new Vector3(2.6f, Thick, 3.5f), _packedSnow);

        Box("WallRunWall", seg, new Vector3(2.4f, Top + 2.5f, z + 1.5f + runLen / 2f),
            new Vector3(0.6f, 7f, runLen), _wall, WallsLayer, "Wall");
        // A facing wall on the left for visual canyon framing (also wall-runnable).
        Box("CanyonWallL", seg, new Vector3(-3.4f, Top + 3.5f, z + 1.5f + runLen / 2f),
            new Vector3(0.6f, 9f, runLen + 4f), _deepIce, WallsLayer, "Wall");

        float end = z + 1.5f + runLen;

        // Landing ledge flush against the end of the wall-run wall + checkpoint.
        Box("CanyonLanding", seg, new Vector3(1.5f, Top - Thick / 2f, end + 2f),
            new Vector3(6f, Thick, 5f), _packedSnow);
        Beacon(seg,new Vector3(1.5f, Top, end + 2f), "Checkpoint_2");
        return end + 5f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 3 — Brittle Ice Bridge: tiles vanish underfoot (KEEP MOVING)
    // ──────────────────────────────────────────────────────────────────────

    static float BuildBrittleBridge(Transform root, float z)
    {
        var seg = Group(root, "3_BrittleBridge");

        const int tiles = 9;
        const float tileLen = 1.6f;
        float zc = z + 1f + tileLen / 2f;
        foreach (var i in Indices(tiles))
        {
            BreakableTile(seg, new Vector3(0, Top, zc), new Vector2(3.2f, tileLen));
            zc += tileLen;
        }
        float end = zc - tileLen / 2f;

        // Solid landing + checkpoint on the far side.
        Platform(seg, new Vector3(0, Top, end + 2f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg,new Vector3(0, Top, end + 1.5f), "Checkpoint_3");
        return end + 4.5f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 4 — Avalanche Chute: outrun a snowball down an ice ramp (SLIDE)
    // ──────────────────────────────────────────────────────────────────────

    static float BuildAvalancheChute(Transform root, float z)
    {
        var seg = Group(root, "4_AvalancheChute");
        float chuteLen = 18f;
        float highY = Top;                 // top of the ramp = shelf height (player steps straight on)
        float lowY = Top - 2f;             // bottom of the ramp; stays above the kill floor (KillY)

        // Sloped ice floor (solid). A long thin slab tilted about X.
        float midZ = z + chuteLen / 2f;
        float midY = (highY + lowY) / 2f;
        float pitch = Mathf.Atan2(highY - lowY, chuteLen) * Mathf.Rad2Deg;
        var floor = Box("ChuteFloor", seg, new Vector3(0, midY, midZ),
            new Vector3(7f, Thick, chuteLen + 1f), _ice);
        floor.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        // Side rails (solid, NOT tagged Wall so the boulder doesn't reset on them).
        for (int s = -1; s <= 1; s += 2)
        {
            var rail = Box($"Rail_{s}", seg, new Vector3(s * 3.3f, midY + 0.8f, midZ),
                new Vector3(0.5f, 2.5f, chuteLen + 1f), _deepIce);
            rail.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        // Backstop at the BOTTOM, tagged "Wall" so the boulder stops & resets there.
        Box("ChuteBackstop", seg, new Vector3(0, lowY + 1f, z + chuteLen + 0.6f),
            new Vector3(7f, 3f, 0.5f), _wall, 0, "Wall");

        // The rolling snowball, parked at the top; gravity rolls it down the ramp.
        Boulder(seg, new Vector3(0, highY + 1.2f, z + 1.5f));

        // Bottom landing + checkpoint.
        Platform(seg, new Vector3(0, lowY, z + chuteLen + 2.5f), new Vector2(8f, 5f), _packedSnow);
        Beacon(seg,new Vector3(0, lowY, z + chuteLen + 2f), "Checkpoint_4");

        // Carry the lowered height forward for the next segment by storing it on a marker.
        _carryTop = lowY;
        return z + chuteLen + 5f;
    }

    static float _carryTop = Top;

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 5 — Floe Crossing: moving platforms over the void, dodge icicles
    // ──────────────────────────────────────────────────────────────────────

    static float BuildFloeCrossing(Transform root, float z)
    {
        var seg = Group(root, "5_FloeCrossing");
        float top = _carryTop;

        // Launch ledge.
        Platform(seg, new Vector3(0, top, z + 1.5f), new Vector2(6f, 4f), _packedSnow);

        // Two moving platforms ferrying the player across a wide void, the second
        // also lifting them back up to the shelf height for the summit.
        Ferry(seg,
            a: new Vector3(-3f, top, z + 6f),
            b: new Vector3(3f, top + 0.5f, z + 10f),
            size: new Vector2(3f, 3f), speed: 0.35f);

        Ferry(seg,
            a: new Vector3(2.5f, top + 0.8f, z + 13f),
            b: new Vector3(-2f, Top, z + 17f),
            size: new Vector2(3f, 3f), speed: 0.3f);

        // Hanging icicle hazards over the crossing for tension.
        SpawnPrefab("Assets/Prefabs/Icicle_Stylized.prefab", seg,
            new Vector3(0, Top + 4f, z + 9f), Quaternion.Euler(180, 0, 0), 1.4f);
        SpawnPrefab("Assets/Prefabs/Icicle_Stylized.prefab", seg,
            new Vector3(-1.5f, Top + 4.5f, z + 14f), Quaternion.Euler(180, 0, 0), 1.2f);

        float end = z + 18f;

        // Arrival ledge + checkpoint back at shelf height.
        Platform(seg, new Vector3(-2f, Top, end + 2f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg,new Vector3(-2f, Top, end + 1.5f), "Checkpoint_5");
        _carryTop = Top;
        return end + 4.5f;
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ACT 2 — second half of the run (new mechanics / harder variations)
    // ══════════════════════════════════════════════════════════════════════

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 7 — Dash Gauntlet: gaps right at the edge of a sprint-jump,
    //  cleanly cleared with the Genji Dash.
    // ──────────────────────────────────────────────────────────────────────

    static float BuildDashGauntlet(Transform root, float z)
    {
        var seg = Group(root, "7_DashGauntlet");

        // Entry pad + checkpoint.
        Platform(seg, new Vector3(0, Top, z + 2.5f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg, new Vector3(0, Top, z + 2f), "Checkpoint_6");

        // Three small floes with ~6m edge gaps (a sprint-jump barely reaches; a dash
        // is comfortable). Entry pad edge ≈ z+5, so the first gap matches the rest.
        float[] xs = { -1.5f, 1.5f, -1f };
        float cz = z + 13f;                 // z+5 edge + 6m gap + 1.5 half-floe
        foreach (var i in Indices(xs.Length))
        {
            Platform(seg, new Vector3(xs[i], Top, cz), new Vector2(3f, 3f), _ice);
            cz += 9f;                       // 3m floe + 6m gap
        }

        // Exit pad one more 6m gap beyond the last floe.
        float ez = (cz - 9f) + 1.5f + 6f + 2.5f;
        Platform(seg, new Vector3(0, Top, ez), new Vector2(7f, 5f), _packedSnow);
        return ez + 3f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 8 — Zigzag Wall-Run: wall-run a right wall to a ledge, cross to
    //  the left side, wall-run a left wall to the exit. Two chained runs.
    // ──────────────────────────────────────────────────────────────────────

    static float BuildZigzagWallRun(Transform root, float z)
    {
        var seg = Group(root, "8_ZigzagWallRun");
        float runLen = 9f;

        // Entry pad hugging the RIGHT wall + checkpoint.
        Box("EntryPad", seg, new Vector3(1.5f, Top - Thick / 2f, z + 2f),
            new Vector3(2.6f, Thick, 4f), _packedSnow);
        Beacon(seg, new Vector3(1.5f, Top, z + 2f), "Checkpoint_7");

        // Run 1 — right wall.
        Box("RightWall", seg, new Vector3(2.4f, Top + 2.5f, z + 4f + runLen / 2f),
            new Vector3(0.6f, 7f, runLen), _wall, WallsLayer, "Wall");

        // Wide mid ledge to land on and reposition to the left side.
        float midZ = z + 4f + runLen + 2.5f;
        Platform(seg, new Vector3(0, Top, midZ), new Vector2(7f, 5f), _packedSnow);

        // Run 2 — left wall (player now hugs the LEFT side).
        float run2Start = midZ + 2.5f;
        Box("LeftWall", seg, new Vector3(-2.4f, Top + 2.5f, run2Start + runLen / 2f),
            new Vector3(0.6f, 7f, runLen), _wall, WallsLayer, "Wall");

        // Exit pad against the end of the left wall + checkpoint.
        float endZ = run2Start + runLen;
        Box("ExitPad", seg, new Vector3(-1.5f, Top - Thick / 2f, endZ + 2f),
            new Vector3(6f, Thick, 5f), _packedSnow);
        return endZ + 5f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 9 — Frozen Steps: an ascending/descending zig-zag of small
    //  platforms. Precise jumps; a super-jump trivialises the tall middle step.
    // ──────────────────────────────────────────────────────────────────────

    static float BuildFrozenSteps(Transform root, float z)
    {
        var seg = Group(root, "9_FrozenSteps");

        Platform(seg, new Vector3(0, Top, z + 2.5f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg, new Vector3(0, Top, z + 2f), "Checkpoint_8");

        // x zig-zags, y rises then falls. Spacing ~4m = normal jumps.
        float[] xs = { -1.2f,  1.2f, -1.2f,  1.0f,  0f };
        float[] ys = {  0.8f,  1.6f,  2.6f,  1.5f,  0.4f };
        float cz = z + 7f;
        foreach (var i in Indices(xs.Length))
        {
            Platform(seg, new Vector3(xs[i], Top + ys[i], cz), new Vector2(2.6f, 2.6f), _ice);
            cz += 4.2f;
        }

        // Exit pad back at shelf height.
        float ez = cz + 1.5f;
        Platform(seg, new Vector3(0, Top, ez), new Vector2(7f, 5f), _packedSnow);
        return ez + 3f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 10 — Serpent Bridge: a long brittle-ice bridge that snakes left
    //  and right, so you must read the path as the tiles collapse behind you.
    // ──────────────────────────────────────────────────────────────────────

    static float BuildSerpentBridge(Transform root, float z)
    {
        var seg = Group(root, "10_SerpentBridge");

        Platform(seg, new Vector3(0, Top, z + 2f), new Vector2(7f, 5f), _packedSnow);
        Beacon(seg, new Vector3(0, Top, z + 1.5f), "Checkpoint_9");

        const int tiles = 13;
        const float tileLen = 1.7f;
        float cz = z + 5f;
        foreach (var i in Indices(tiles))
        {
            float x = Mathf.Sin(i * 0.7f) * 1.8f;     // gentle serpentine
            BreakableTile(seg, new Vector3(x, Top, cz), new Vector2(3f, tileLen));
            cz += tileLen;
        }

        // Far landing + checkpoint.
        float ez = cz + 1.5f;
        Platform(seg, new Vector3(0, Top, ez), new Vector2(7f, 5f), _packedSnow);
        return ez + 3f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Segment 11 — The Summit: wall-climb + super-jump to the finish portal
    // ──────────────────────────────────────────────────────────────────────

    static float BuildSummit(Transform root, float z)
    {
        var seg = Group(root, "11_Summit");

        // A tall climbable ice face (Walls layer => the player can wall-climb it).
        float faceH = 7f;
        Box("ClimbFace", seg, new Vector3(0, Top + faceH / 2f, z + 1.5f),
            new Vector3(8f, faceH, 0.6f), _deepIce, WallsLayer, "Wall");

        // A mid ledge to vault onto after the climb.
        float ledgeTop = Top + faceH;
        Box("SummitLedge", seg, new Vector3(0, ledgeTop - Thick / 2f, z + 3f),
            new Vector3(8f, Thick, 5f), _packedSnow);

        // Final super-jump up to the victory platform with the portal.
        float summitTop = ledgeTop + 4f;
        Platform(seg, new Vector3(0, summitTop, z + 7f), new Vector2(8f, 6f), _ice);
        Beacon(seg,new Vector3(0, summitTop, z + 5.5f), "Checkpoint_Summit");

        // Decorative tower behind the summit as a landmark.
        SpawnPrefab("Assets/Prefabs/StylizedTower.prefab", seg,
            new Vector3(0, summitTop, z + 10f), Quaternion.identity, 1f);

        // The finish portal — a glowing trigger that loads the next scene.
        var portal = Box("FinishPortal", seg, new Vector3(0, summitTop + 1.6f, z + 8.5f),
            new Vector3(3f, 3.2f, 0.4f), _portalMat);
        var pc = portal.GetComponent<BoxCollider>();
        pc.isTrigger = true;
        var lt = portal.AddComponent<LevelTransition>();
        lt.nextLevelName = "MainMenu";   // change to the next level you want to load

        return z + 12f;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Building-block helpers
    // ──────────────────────────────────────────────────────────────────────

    static Transform Group(Transform parent, string name)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        return g.transform;
    }

    /// <summary>A plain box. center is the WORLD-of-local centre; size is full extents.</summary>
    static GameObject Box(string name, Transform parent, Vector3 localCenter, Vector3 size,
                          Material mat, int layer = 0, string tag = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.layer = layer;
        if (!string.IsNullOrEmpty(tag)) go.tag = tag;
        return go;
    }

    /// <summary>A platform slab whose TOP surface sits at topCenter.y.</summary>
    static GameObject Platform(Transform parent, Vector3 topCenter, Vector2 sizeXZ, Material mat)
    {
        return Box("Platform", parent,
            new Vector3(topCenter.x, topCenter.y - Thick / 2f, topCenter.z),
            new Vector3(sizeXZ.x, Thick, sizeXZ.y), mat);
    }

    /// <summary>
    /// One big crevasse floor under the entire course. It is a trigger (DeathZone),
    /// so falling anywhere — sideways, short, or long — respawns you at the last
    /// checkpoint. Sits at KillY: below every platform but above the original ground,
    /// so the player never reaches (and survives on) the old flat terrain.
    /// Its visible dark-ice surface also reads as a deep frozen chasm.
    /// </summary>
    static void BuildKillFloor(Transform root, float totalLength)
    {
        float len = totalLength + 40f;     // generous margin front and back
        var go = Box("CrevasseFloor", root,
            new Vector3(0, KillTopY - KillThickness / 2f, totalLength / 2f - 10f),
            new Vector3(90f, KillThickness, len), _hazard);
        go.GetComponent<BoxCollider>().isTrigger = true;
        go.AddComponent<DeathZone>();      // uses Checkpoint.IsPlayer (tag OR PlayerMovement)
    }

    static void Beacon(Transform parent, Vector3 topCenter, string name)
    {
        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = name;
        beacon.transform.SetParent(parent, false);
        beacon.transform.localPosition = new Vector3(topCenter.x, topCenter.y + 1f, topCenter.z);
        beacon.transform.localScale = new Vector3(0.15f, 1.2f, 0.15f);
        beacon.GetComponent<Renderer>().sharedMaterial = _checkpointMat;

        // Radius scales by the beacon's 0.15 X-scale, so use a large local value to
        // get a ~2.5m world trigger the player can't miss when crossing.
        var col = beacon.GetComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.radius = 17f;
        col.height = 4f;

        var cp = beacon.AddComponent<Checkpoint>();
        cp.beamRenderer = beacon.GetComponent<Renderer>();
        cp.spawnOffset = new Vector3(0, 0.2f, 0);
    }

    /// <summary>
    /// A brittle ice tile: the player can stand on it, but a moment after stepping on
    /// it collapses, then respawns. Uses BrittleIce (delayed collapse) instead of
    /// BreakableSnow (instant) so a multi-tile bridge is actually crossable.
    /// </summary>
    static void BreakableTile(Transform parent, Vector3 topCenter, Vector2 sizeXZ)
    {
        // Primitive cube already has a solid BoxCollider -> becomes BrittleIce.solidCollider.
        var tile = Box("BrittleTile", parent,
            new Vector3(topCenter.x, topCenter.y - Thick / 2f, topCenter.z),
            new Vector3(sizeXZ.x, Thick, sizeXZ.y), _ice);

        // A second, trigger collider sitting just on the surface, so the tile only
        // arms when the player actually steps ON it — not when brushing past the side.
        var trig = tile.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(0.9f, 2.2f, 0.9f);   // local (scaled by slab)
        trig.center = new Vector3(0, 1.1f, 0);

        tile.AddComponent<BrittleIce>();             // defaults solidCollider to the solid box, visual to self
    }

    static void Ferry(Transform parent, Vector3 a, Vector3 b, Vector2 size, float speed)
    {
        var plat = Box("MovingFloe", parent, a, new Vector3(size.x, Thick, size.y), _ice);

        // Top trigger so the script can carry the player.
        var trig = plat.AddComponent<BoxCollider>();
        trig.isTrigger = true;
        trig.size = new Vector3(1f, 2.4f, 1f);
        trig.center = new Vector3(0, 1.2f, 0);

        // Endpoint markers live on the segment root (NOT the platform) so they stay put.
        var pA = new GameObject("PointA"); pA.transform.SetParent(parent, false); pA.transform.localPosition = a;
        var pB = new GameObject("PointB"); pB.transform.SetParent(parent, false); pB.transform.localPosition = b;

        var mp = plat.AddComponent<MovingPlatform>();
        mp.pointA = pA.transform;
        mp.pointB = pB.transform;
        mp.speed = speed;
    }

    static void Boulder(Transform parent, Vector3 localPos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CrushingBoulder.prefab");
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
        }
        else
        {
            // Fallback if the prefab is missing: a plain sphere with the component.
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Snowball";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 2f;
            go.GetComponent<Renderer>().sharedMaterial = _packedSnow;
            go.AddComponent<Rigidbody>();
            go.AddComponent<CrushingBoulder>();
        }

        // The boulder needs a startPosition to reset to. Give it an independent marker.
        var start = new GameObject("BoulderStart");
        start.transform.SetParent(parent, false);
        start.transform.localPosition = localPos;
        var cb = go.GetComponent<CrushingBoulder>();
        if (cb != null) cb.startPosition = start.transform;
    }

    static GameObject SpawnPrefab(string path, Transform parent, Vector3 localPos, Quaternion localRot, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogWarning($"Prefab not found: {path}"); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Scene wiring
    // ──────────────────────────────────────────────────────────────────────

    static Transform FindPlayer()
    {
        var pm = Object.FindFirstObjectByType<PlayerMovement>();
        if (pm != null) return pm.transform;
        var tagged = GameObject.FindWithTag("Player");
        return tagged != null ? tagged.transform : null;
    }

    static void EnsureCheckpointManager(Transform player)
    {
        var cm = Object.FindFirstObjectByType<CheckpointManager>();
        if (cm == null)
        {
            var go = new GameObject("CheckpointManager");
            cm = go.AddComponent<CheckpointManager>();
            cm.deathYThreshold = -50f;
            cm.respawnDelay = 0.4f;
            Debug.Log("Created a CheckpointManager (none existed).");
        }
        if (cm.player == null && player != null) cm.player = player;
    }

    static void DisableYeti()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.activeSelf && go.name.ToLowerInvariant().Contains("yeti"))
            {
                go.SetActive(false);
                Debug.Log($"Disabled '{go.name}' (yeti removed from the level).");
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Materials
    // ──────────────────────────────────────────────────────────────────────

    static void BuildMaterials()
    {
        // Platforms get a faint self-illumination (glow) so they stay clearly visible
        // against the dark snowy sky — the level is dimly lit, and a flat unlit slab
        // can read as "disappearing". Walls/hazard stay matte.
        _ice          = Mat(new Color(0.72f, 0.88f, 0.98f), smooth: 0.85f, glow: 0.18f);
        _packedSnow   = Mat(new Color(0.95f, 0.97f, 1.0f),  smooth: 0.2f,  glow: 0.22f);
        _deepIce      = Mat(new Color(0.35f, 0.55f, 0.75f), smooth: 0.7f);
        _wall         = Mat(new Color(0.6f, 0.78f, 0.92f),  smooth: 0.6f,  glow: 0.12f);
        _hazard       = Mat(new Color(0.1f, 0.18f, 0.32f),  smooth: 0.9f);
        _checkpointMat = Emissive(new Color(1f, 0.15f, 0.05f));
        _portalMat     = Emissive(new Color(0.3f, 0.9f, 1f));
    }

    static Material Mat(Color c, float metallic = 0f, float smooth = 0.5f, float glow = 0f)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(shader) { color = c };
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * glow);
        }
        return m;
    }

    static Material Emissive(Color c)
    {
        var m = Mat(c);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 2f);
        return m;
    }

    static IEnumerable<int> Indices(int n) { for (int i = 0; i < n; i++) yield return i; }
}
