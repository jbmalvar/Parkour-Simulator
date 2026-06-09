using UnityEngine;

public class BreakableSnow : MonoBehaviour
{
    [SerializeField] private GameObject snowVisual;
    [SerializeField] private Collider solidCollider;

    private void Start()
    {
        if (snowVisual == null) snowVisual = this.gameObject;
        if (solidCollider == null) solidCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Collision with " + other.name);
        if (other.CompareTag("Player") || other.GetComponentInParent<CharacterController>() != null)
        {
            if (solidCollider != null) solidCollider.enabled = false;
            if (snowVisual != null) snowVisual.SetActive(false);
        }
    }
}
