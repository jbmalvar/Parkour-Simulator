using System.Collections;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    [Header("References")]
    public Transform player;
    public ScreenFade screenFade;

    [Header("Settings")]
    public float deathYThreshold = -20f;
    public float respawnDelay = 0.4f;

    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private bool isRespawning = false;
    private CharacterController characterController;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        ResolvePlayer();
        if (player != null)
        {
            characterController = player.GetComponent<CharacterController>();
            checkpointPosition = player.position;
            checkpointRotation = player.rotation;
        }
    }

    // Find the player even if the inspector reference is empty or got broken (e.g. the
    // rig was swapped in from another scene). Looks up the PlayerMovement, then the tag.
    private void ResolvePlayer()
    {
        if (player != null) return;
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) { player = pm.transform; return; }
        var tagged = GameObject.FindWithTag("Player");
        if (tagged != null) player = tagged.transform;
    }

    void Update()
    {
        if (!isRespawning && player != null && player.position.y < deathYThreshold)
            StartCoroutine(Respawn());
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
    }

    public void TriggerDeath()
    {
        if (!isRespawning)
            StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        isRespawning = true;

        ResolvePlayer();                 // make sure we still have a valid player
        if (player == null)
        {
            Debug.LogWarning("CheckpointManager: no player found to respawn.");
            isRespawning = false;
            yield break;
        }
        if (characterController == null) characterController = player.GetComponent<CharacterController>();

        if (screenFade != null)
            yield return StartCoroutine(screenFade.FadeOut());

        if (characterController != null) characterController.enabled = false;
        player.position = checkpointPosition;
        player.rotation = checkpointRotation;
        if (characterController != null) characterController.enabled = true;

        yield return new WaitForSeconds(respawnDelay);

        if (screenFade != null)
            yield return StartCoroutine(screenFade.FadeIn());

        isRespawning = false;
    }
}
