using UnityEngine;

public class UIManager : MonoBehaviour
{
    // This creates a global access point to your UI
    public static UIManager Instance { get; private set; }

    void Awake()
    {
        // If a UIManager already exists in the scene, destroy this new one immediately
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // If this is the first UIManager, claim the throne and protect it from scene loads
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}