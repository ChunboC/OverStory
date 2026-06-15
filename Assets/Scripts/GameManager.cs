using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 60f;
    public TMP_Text timerText;

    [Header("Game Message UI")]
    public GameObject gameMessageObject;
    public TMP_Text gameMessageText;

    private bool gameOver = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (gameMessageObject != null)
        {
            gameMessageObject.SetActive(false); // hides gameover message at the start
        }
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
