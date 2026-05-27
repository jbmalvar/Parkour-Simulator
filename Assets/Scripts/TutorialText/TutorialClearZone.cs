using UnityEngine;

public class TutorialClearZone : MonoBehaviour
{
    public string promptIDToClear; 

    private void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Something hit the clear zone: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            // Debug.Log("It was the player! Attempting to clear ID: " + promptIDToClear);
            TutorialManager.Instance.HidePrompt(promptIDToClear);
            gameObject.SetActive(false); 
        }
    }
}