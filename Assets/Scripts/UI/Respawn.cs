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
        retryScreen.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ReturnToCheckpoint()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        retryScreen.SetActive(false);
        player.position = checkpoints[currentCheckpoint].position;
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }

    public void ReachedCheckpoint(int index)
    {
        if (index > currentCheckpoint)
            currentCheckpoint = index;
    }
}