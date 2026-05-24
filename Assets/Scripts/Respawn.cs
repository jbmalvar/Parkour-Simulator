using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Respawn : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Checkpoints")]
    public Transform[] checkpoints;
    private int currentCheckpoint = 0;

    [Header("Retry UI")]
    public GameObject retryScreen;
    public Button returnToCheckpointButton;
    public Button restartLevelButton;
    public Button mainMenuButton;

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        retryScreen.SetActive(false);

        returnToCheckpointButton.onClick.AddListener(ReturnToCheckpoint);
        restartLevelButton.onClick.AddListener(RestartLevel);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
    }

    public void PlayerDied()
    {
        // Show retry screen
        retryScreen.SetActive(true);
        Time.timeScale = 0f; // Pause the game
    }

    void ReturnToCheckpoint()
    {
        Time.timeScale = 1f;
        retryScreen.SetActive(false);
        player.position = checkpoints[currentCheckpoint].position;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // Call this when player reaches a checkpoint
    public void ReachedCheckpoint(int index)
    {
        if (index > currentCheckpoint)
            currentCheckpoint = index;
    }
}