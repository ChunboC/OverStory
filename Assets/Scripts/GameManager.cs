using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 60f;
    public TMP_Text timerText;
    public TMP_Text shamrockText;

    [Header("Game Message UI")]
    public GameObject gameMessageObject;
    public TMP_Text gameMessageText;

    [Header("Menus and Navigation UI")]
    public GameObject startMenuUI;
    public GameObject selectedStartButton;
    public GameObject pauseMenuUI;
    public GameObject selectedPauseButton;
    public GameObject winMenuUI;
    public GameObject selectedWinButton;
    public GameObject loseMenuUI;
    public GameObject selectedLoseButton;

    [Header("Input Settings")]
    public PlayerInput playerInput;

    [Header("Pre-Game Intro Setup")]
    [SerializeField] private UnityEngine.Playables.PlayableDirector preGameDirector;
    [SerializeField] private GameObject introSceneCameras;
    [SerializeField] private Transform cameraAnchor;

    [Header("Game Targets to Enable")]
    [SerializeField] private GameObject playerGameObject;
    [SerializeField] private GameObject enemyGameObject;

    private UnityEngine.AI.NavMeshAgent enemyAgent;

    private bool isPaused;
    private bool gameOver = false;
    public bool isIntroPlaying = false;

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

        if (enemyGameObject != null) enemyAgent = enemyGameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (preGameDirector != null)
        {
            isIntroPlaying = true;
            preGameDirector.stopped += OnIntroSequenceFinished;
            preGameDirector.Play();
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    private void OnIntroSequenceFinished(UnityEngine.Playables.PlayableDirector director)
    {
        preGameDirector.stopped -= OnIntroSequenceFinished;
        StartGameplay();
    }

    private void StartGameplay()
    {
        isIntroPlaying = false;

        if (playerGameObject != null)
        {
            var scripts = playerGameObject.GetComponents<MonoBehaviour>();
            foreach (var script in scripts) script.enabled = true;
        }

        if (enemyGameObject != null)
        {
            var scripts = enemyGameObject.GetComponents<MonoBehaviour>();
            foreach (var script in scripts) script.enabled = true;

            if (enemyAgent != null) enemyAgent.enabled = true;
        }

        if (introSceneCameras != null)
        {
            introSceneCameras.SetActive(false);
        }

        if (cameraAnchor != null)
        {
            Camera.main.transform.localPosition = cameraAnchor.localPosition;
            Camera.main.transform.localRotation = cameraAnchor.localRotation;
        }

        var brain = Camera.main.GetComponent("CinemachineBrain") as MonoBehaviour;
        if (brain != null)
        {
            brain.enabled = false;
            Debug.Log("Cinemachine Brain explicitly disabled to restore child camera settings.");
        }

        Debug.Log("Pre-game animation finished");
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
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("UI");
                playerInput.ActivateInput();
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Clear previous selection
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectedPauseButton);
        }
        else
        {
            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("Player");
                playerInput.ActivateInput();
            }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Clear selection
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ResumeGame()
    {
        TogglePause();
    }

    public void RestartGame()
    {
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
            playerInput.ActivateInput();
        }
        Time.timeScale = 1f; // Always unfreeze time before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("StartMenu");
    }

    public void ExitGame()
    {
        Debug.Log("Exiting scene");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // lose when time runs out
    void Update()
    {
        if (isIntroPlaying)
        {
            bool mouseClick = UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
            bool controllerClick = UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame;

            if (mouseClick || controllerClick)
            {
                SkipPreGameIntro();
                return;
            }
        }

        if (isIntroPlaying || gameOver) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            LoseGame();
        }

        UpdateTimerUI();

        if (playerGameObject != null)
        {
            ProjectileThrower thrower = playerGameObject.GetComponent<ProjectileThrower>();
            UpdateShamrockCount(thrower.ShamrockCount);
        }
    }

    private void SkipPreGameIntro()
    {
        if (preGameDirector != null)
        {
            preGameDirector.stopped -= OnIntroSequenceFinished;

            preGameDirector.time = preGameDirector.duration;
            preGameDirector.Evaluate();
            preGameDirector.Stop();
        }

        StartGameplay();
    }

    // update remaining time
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.CeilToInt(timeRemaining).ToString();
        }
    }

    void UpdateShamrockCount(int shamCount)
    {
        if (shamrockText != null)
        {
            shamrockText.text = $"    x{shamCount}";
        }
    }

    // check timer when game ends and show win message with time left
    public void WinGame()
    {
        if (gameOver) return;

        gameOver = true;
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
            playerInput.ActivateInput();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Clear previous selection
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectedWinButton);
        SceneManager.LoadScene("WinScene");
    }

    public void LoseGame()
    {
        if (gameOver) return;

        gameOver = true;
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
            playerInput.ActivateInput();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Clear previous selection
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectedLoseButton);
        SceneManager.LoadScene("LoseScene");
    }
}
