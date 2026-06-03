using UnityEngine;

public class SimpleMusic : MonoBehaviour
{
    private static SimpleMusic instance;

    void Awake()
    {
        // If an instance already exists, destroy this one
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        // Otherwise, make this the instance and don't destroy it on level load
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}