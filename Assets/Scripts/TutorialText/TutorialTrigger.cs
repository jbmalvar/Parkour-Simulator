using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Prompt Settings")]
    public string promptID; 
    [TextArea(3, 5)]
    public string promptMessage;
    
    private bool hasShown = false;

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Something walked into the START zone: " + other.gameObject.name);

        if (!hasShown && other.CompareTag("Player"))
        {
            // Debug.Log("Player detected! Sending message to Manager for ID: " + promptID);
            TutorialManager.Instance.ShowPrompt(promptMessage, promptID);
            hasShown = true; 
        }
        else if (hasShown)
        {
            // Debug.Log("Ignored: This prompt was already shown once.");
        }
    }
}