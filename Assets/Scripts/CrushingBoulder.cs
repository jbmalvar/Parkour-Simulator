using UnityEngine;

public class CrushingBoulder : MonoBehaviour
{
    public float rollSpeed = 5f;
    public float resetTime = 3f;
    public Transform startPosition;

    private bool isRolling = true;
    private float resetTimer = 0f;

    void Update()
    {
        if (isRolling)
        {
            // Roll forward
            transform.Translate(Vector3.forward * rollSpeed * Time.deltaTime);
            // Spin visually
            transform.Rotate(Vector3.right * rollSpeed * 50 * Time.deltaTime);
        }
        else
        {
            resetTimer += Time.deltaTime;
            if (resetTimer >= resetTime)
                ResetBoulder();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Will connect to James's respawn system later
            Debug.Log("Player hit by boulder - trigger respawn");
        }

        if (other.CompareTag("Wall"))
        {
            isRolling = false;
            resetTimer = 0f;
        }
    }

    void ResetBoulder()
    {
        transform.position = startPosition.position;
        transform.rotation = Quaternion.identity;
        isRolling = true;
        resetTimer = 0f;
    }
}