using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.InputSystem; 

public class PauseMenu : MonoBehaviour
{
    // We will use this variable to tell your camera to stop moving
    public static bool GameIsPaused = false;
    public GameObject pauseMenuUI;

    void Start()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; 
        GameIsPaused = true;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; 
        GameIsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    public void QuitGame()
    {
        Time.timeScale = 1f; 
        GameIsPaused = false;
        SceneManager.LoadScene("MainMenu"); 
    }
}