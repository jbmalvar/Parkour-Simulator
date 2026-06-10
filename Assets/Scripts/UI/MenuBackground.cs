using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

// Loads a gameplay scene additively to use as a living backdrop behind the main
// menu, then strips everything that would interfere — gameplay scripts, the player,
// extra cameras/audio, and crucially the loaded scene's own HUD canvas + EventSystem
// (which otherwise cover and steal clicks from the menu) and its cursor lock. What's
// left is just the 3D scenery, sitting behind the menu's overlay buttons.
//
// Put it on a Main Menu object and set backgroundScene (default "SpawnMap"). Pair
// with MenuCameraDrift for slow motion and MenuButterfly for life.
public class MenuBackground : MonoBehaviour
{
    public string backgroundScene = "SpawnMap";
    [Tooltip("Hide the player character so only scenery shows.")]
    public bool hidePlayer = true;
    [Tooltip("Move the menu camera to where the loaded scene's camera looks, so it frames " +
             "the scenery automatically. Turn off if you want to position the camera by hand.")]
    public bool frameFromSceneCamera = true;

    // Component type names to switch off in the backdrop so nothing "plays".
    static readonly string[] DisableComponents =
    {
        "PlayerMovement", "PlayerAbilities", "PlayerHealth", "PlayerMana", "PlayerStamina",
        "ParkourCamera", "MouseLook", "GestureCaster", "CheckpointManager", "PauseMenu",
    };

    void Start()
    {
        if (!SceneManager.GetSceneByName(backgroundScene).isLoaded)
            StartCoroutine(LoadBackdrop());
    }

    IEnumerator LoadBackdrop()
    {
        yield return SceneManager.LoadSceneAsync(backgroundScene, LoadSceneMode.Additive);

        Scene scene = SceneManager.GetSceneByName(backgroundScene);
        if (!scene.IsValid()) yield break;

        // Capture where the loaded scene's camera looks, so we can frame the menu camera
        // onto the actual scenery (otherwise it stares at empty space = grey background).
        bool haveVantage = false;
        Vector3 vantagePos = default;
        Quaternion vantageRot = default;

        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            {
                // Prefer the main view camera; fall back to the first one we see.
                if (!haveVantage || cam.CompareTag("MainCamera"))
                {
                    vantagePos = cam.transform.position;
                    vantageRot = cam.transform.rotation;
                    haveVantage = true;
                    if (cam.CompareTag("MainCamera")) break;
                }
            }
        }

        foreach (var root in scene.GetRootGameObjects())
        {
            // Turn off scripts that would run the game in the background.
            foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) continue;
                string n = mb.GetType().Name;
                foreach (var bad in DisableComponents)
                {
                    if (n == bad || n.Contains("Leaderboard") || n.Contains("Network"))
                    {
                        mb.enabled = false;
                        break;
                    }
                }
            }

            // Only the menu's camera + audio listener should be authoritative.
            foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                cam.gameObject.SetActive(false);
            foreach (var al in root.GetComponentsInChildren<AudioListener>(true))
                al.enabled = false;

            // Hide the backdrop's HUD (any screen-space Canvas) so it doesn't draw over
            // the menu, and kill its EventSystem so the menu's EventSystem owns input.
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                if (canvas.renderMode != RenderMode.WorldSpace)
                    canvas.gameObject.SetActive(false);
            foreach (var es in root.GetComponentsInChildren<EventSystem>(true))
                es.gameObject.SetActive(false);

            // Remove the player character — we just want scenery.
            if (hidePlayer)
                foreach (var cc in root.GetComponentsInChildren<CharacterController>(true))
                    cc.transform.root.gameObject.SetActive(false);
        }

        // Frame the menu camera onto the scenery (the camera with MenuCameraDrift, else
        // Camera.main) and show the sky, so we see grass/houses instead of grey.
        if (frameFromSceneCamera && haveVantage)
        {
            var drift = FindAnyObjectByType<MenuCameraDrift>();
            Camera menuCam = drift != null ? drift.GetComponent<Camera>() : Camera.main;
            if (menuCam != null)
            {
                menuCam.transform.SetPositionAndRotation(vantagePos, vantageRot);
                menuCam.clearFlags = CameraClearFlags.Skybox;
                if (drift != null) drift.ResetBase();   // sway around the new vantage
            }
        }

        // The loaded scene's player/camera scripts lock the cursor as they start, which
        // makes the menu unclickable. Force it back now that they're disabled.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
