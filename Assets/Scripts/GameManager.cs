using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 60f;
    public TMP_Text timerText;

    [Header("Game Message UI")]
    public GameObject gameMessageObject;
    public TMP_Text gameMessageText;

    [Header("Menus and Navigation UI")]
    public GameObject startMenuUI;
    public GameObject selectedStartButton;
    public GameObject pauseMenuUI;
    public GameObject selectedPauseButton;

    private bool isPaused;
    private bool gameOver = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (gameMessageObject != null)
        {
            gameMessageObject.SetActive(false); // hides gameover message at the start
        }

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        if (startMenuUI != null)
        {
            startMenuUI.SetActive(true);

            // Highlight the start button for controllers
            if (EventSystem.current != null && selectedStartButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(selectedStartButton);
            }
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        // Freeze time when paused, resume normal speed when unpaused
        Time.timeScale = isPaused ? 0f : 1f;

        // Turn the pause menu on or off
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(isPaused);
        }

        if (isPaused)
        {
            // Clear any previous selection first to prevent bugs
            EventSystem.current.SetSelectedGameObject(null);

            // Highlight the resume button automatically so gamepad/keyboard users can navigate
            EventSystem.current.SetSelectedGameObject(selectedPauseButton);
        }
        else
        {
            // Clear selection when unpausing so UI highlights don't get stuck on screen
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Always unfreeze time before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        Debug.Log("Exiting scene");
        Application.Quit();
    }

    // lose when time runs out
    void Update()
    {
        if (gameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            LoseGame();
        }

        UpdateTimerUI();
    }

    // update remaining time
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    // check timer when game ends and show win message with time left
    public void WinGame()
    {
        if (gameOver) return;

        gameOver = true;

        if (gameMessageObject != null)
        {
            gameMessageObject.SetActive(true);
        }

        if (gameMessageText != null)
        {
            gameMessageText.text = "You Win!";
        }

        Time.timeScale = 0f;
    }

    public void LoseGame()
    {
        if (gameOver) return;

        gameOver = true;

        if (gameMessageObject != null)
        {
            gameMessageObject.SetActive(true);
        }

        if (gameMessageText != null)
        {
            gameMessageText.text = "You Lose!";
        }

        Time.timeScale = 0f;
    }
}
