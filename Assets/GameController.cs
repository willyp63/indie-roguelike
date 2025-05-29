using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Game State")]
    public bool isPaused = false;

    private float previousTimeScale = 1f;

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Space bar to pause/unpause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePause();
        }

        // R key to restart scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }

        // Optional: ESC key as alternative pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        Debug.Log("Game Paused");
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = previousTimeScale;

        Debug.Log("Game Resumed");
    }

    public void RestartScene()
    {
        // Make sure time scale is back to normal before restarting
        Time.timeScale = 1f;

        // Reload the current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);

        Debug.Log("Scene Restarted");
    }

    // Optional: Prevent issues when game object is destroyed while paused
    void OnDestroy()
    {
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }
}
