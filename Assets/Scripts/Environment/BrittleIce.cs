using System.Collections;
using UnityEngine;

/// <summary>
/// A bridge tile that collapses a short moment AFTER the player steps on it,
/// then restores itself so the bridge can be retried.
///
/// Unlike <see cref="BreakableSnow"/> (which vanishes the instant it is touched —
/// fine for a single trap block, but it makes a multi-tile bridge impossible to
/// cross because tiles drop out from under you), the grace delay lets you run
/// across if you keep your momentum, and the respawn lets you try again.
///
/// Setup: a solid collider to stand on + a trigger collider to detect the player.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BrittleIce : MonoBehaviour
{
    [Tooltip("Renderers to hide while collapsed. Defaults to all on this object.")]
    [SerializeField] private GameObject visual;
    [Tooltip("The solid surface the player stands on. Defaults to the first non-trigger collider.")]
    [SerializeField] private Collider solidCollider;

    [Header("Timing")]
    public float breakDelay = 0.35f;    // grace time between the first touch and the collapse
    public float respawnDelay = 4f;     // time until the tile comes back for another attempt
    public float shakeAmount = 0.05f;   // wobble while it's about to break (telegraph)

    private bool triggered = false;
    private Vector3 visualHome;
    private Transform visualTf;

    void Start()
    {
        if (visual == null) visual = gameObject;
        visualTf = visual.transform;
        visualHome = visualTf.localPosition;

        if (solidCollider == null)
        {
            foreach (var c in GetComponents<Collider>())
                if (!c.isTrigger) { solidCollider = c; break; }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null)
            StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        triggered = true;

        // Telegraph the break with a brief shake.
        float t = 0f;
        while (t < breakDelay)
        {
            t += Time.deltaTime;
            visualTf.localPosition = visualHome +
                new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * shakeAmount;
            yield return null;
        }
        visualTf.localPosition = visualHome;

        // Collapse: drop the floor and hide it. We disable renderers (not the whole
        // GameObject) so this coroutine keeps running to respawn the tile.
        if (solidCollider != null) solidCollider.enabled = false;
        SetVisible(false);

        yield return new WaitForSeconds(respawnDelay);

        // Restore for the next attempt.
        SetVisible(true);
        if (solidCollider != null) solidCollider.enabled = true;
        triggered = false;
    }

    private void SetVisible(bool on)
    {
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
            r.enabled = on;
    }
}
