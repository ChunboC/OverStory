using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeRemaining = 90f;
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
    public GameObject creditsMenuUI;
    public GameObject selectedCreditsButton;
    public GameObject selectedBackButton;

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
    private static bool isIntroPlaying;
    public static bool IsIntroPlaying => isIntroPlaying;

    public bool gamePlaying = false;

    [Header("Run Flow")]
    [Tooltip("Scene the Start button loads. Set to Level1_Scene once the level is in the build list.")]
    public string firstSceneName = "MainScene";

    [Header("Fall Penalty Settings")]
    public TMP_Text penaltyText;
    public float penaltyTextDuration = 2f;
    private Coroutine penaltyTextCoroutine;

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

        if (creditsMenuUI != null)
        {
            creditsMenuUI.SetActive(false);
        }

        if (enemyGameObject != null) enemyAgent = enemyGameObject.GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (preGameDirector != null)
        {
            isIntroPlaying = true;
            preGameDirector.stopped += OnIntroSequenceFinished;
            preGameDirector.Play();
        }
        else
        {
            // Scenes without an intro cutscene (Level1_Scene) would otherwise
            // never leave the pre-game state, freezing the timer and the HUD.
            StartGameplay();
        }

        if (penaltyText != null)
        {
            penaltyText.gameObject.SetActive(false);
        }
    }

    public void StartGame()
    {
        // A run starts here, so drop anything banked by a previous attempt.
        RunProgress.ResetRun();
        SceneManager.LoadScene(string.IsNullOrEmpty(firstSceneName) ? "TutorialVillage" : firstSceneName);
    }

    public void PlayAgainLevel1()
    {
        Time.timeScale = 1f;
        RunProgress.ResetRun();
        SceneManager.LoadScene("Level1_Scene");
    }

    private void OnIntroSequenceFinished(UnityEngine.Playables.PlayableDirector director)
    {
        preGameDirector.stopped -= OnIntroSequenceFinished;
        StartGameplay();
    }

    private void StartGameplay()
    {
        isIntroPlaying = false;
        gamePlaying = true;

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

        if (Camera.main != null)
        {
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
        gamePlaying = false;
        RunProgress.ResetRun();
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

        if (isIntroPlaying || gameOver || !gamePlaying) return;

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
            if (thrower != null) UpdateShamrockCount(thrower.ShamrockCount);
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
        gamePlaying = false;
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
        gamePlaying = false;
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

    //This function is called when player falls off the map and time is deducted
    public void DeductTime(float secondsToDeduct)
    {

        if (isIntroPlaying || gameOver || !gamePlaying) return;
        
        timeRemaining -= secondsToDeduct;

        if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            LoseGame();
        }

        ShowPenaltyWarning($"Fall Penalty:\n-{secondsToDeduct} Seconds!");
    }

    private void ShowPenaltyWarning(string message)
    {
        if (penaltyText == null) return;

        if (penaltyTextCoroutine != null) //Prevent it from having multiple overlapping in case player falls off repeatedly
        {
            StopCoroutine(penaltyTextCoroutine);
        }

        penaltyTextCoroutine = StartCoroutine(DisplayPenaltyRoutine(message));
    }

    private IEnumerator DisplayPenaltyRoutine(string message)
    {
        penaltyText.text = message;
        penaltyText.color = Color.red;
        penaltyText.gameObject.SetActive(true);

        yield return new WaitForSeconds(penaltyTextDuration);

        penaltyText.gameObject.SetActive(false);
    }

    public void ShowCredits()
    {
        if (creditsMenuUI == null)
        {
            return;
        }

        creditsMenuUI.SetActive(true);

        // Wait one frame for the Credits panel to become active,
        // then select its Back button.
        StartCoroutine(
            SelectButtonNextFrame(selectedBackButton)
        );
    }


    public void HideCredits()
    {
        if (creditsMenuUI != null)
        {
            creditsMenuUI.SetActive(false);
        }

        // Return controller selection to the Credits button.
        StartCoroutine(
            SelectButtonNextFrame(selectedCreditsButton)
        );
    }

    private IEnumerator SelectButtonNextFrame(
    GameObject buttonToSelect)
    {
        yield return null;

        if (EventSystem.current == null ||
            buttonToSelect == null)
        {
            yield break;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(
            buttonToSelect
        );
    }
}
