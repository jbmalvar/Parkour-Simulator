using UnityEngine;
using TMPro;

public class CheckpointNotification : MonoBehaviour
{
    public TextMeshProUGUI checkpointText;
    public string message = "Checkpoint 0";
    public float displayTime = 2f;

    private float timer = 0f;
    private bool isShowing = false;

    void Update()
    {
        if (isShowing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                checkpointText.gameObject.SetActive(false);
                isShowing = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            checkpointText.gameObject.SetActive(true);
            checkpointText.text = message;
            timer = displayTime;
            isShowing = true;
        }
    }

    public void ShowNotification()
    {
        checkpointText.gameObject.SetActive(true);
        checkpointText.text = message;
        timer = displayTime;
        isShowing = true;
    }
}