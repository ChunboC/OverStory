using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementTrainerNPC : MonoBehaviour
{
    private enum TutorialStep
    {
        Waiting,
        DoubleJump,
        AirDash,
        CollectShamrock,
        Complete
    }

    [Header("NPC")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TMP_Text tutorialText;

    [Header("Messages")]
    [TextArea]
    [SerializeField]
    private string doubleJumpMessage =
        "Press Space or the South gamepad button twice to double jump!";

    [TextArea]
    [SerializeField]
    private string airDashMessage =
        "Great! While airborne, press Left Shift or the West gamepad button to dash!";

    [TextArea]
    [SerializeField]
    private string collectShamrockMessage =
        "Great! Now use your double jump and air dash to collect the shamrock on top of the fountain!";

    [TextArea]
    [SerializeField]
    private string completeMessage =
        "Excellent! You mastered the double jump, air dash, and collected the shamrock! Good luck out there!";

    

    [Header("Text Reveal")]
    [SerializeField] private float letterDelay = 0.03f;

    [SerializeField] private float finalMessageHoldTime = 2.5f;

    private Coroutine completionRoutine;

    private PlayerController playerController;
    private Transform player;

    private InputAction jumpAction;
    private InputAction dashAction;
    private bool fountainShamrockCollected;

    private TutorialStep currentStep = TutorialStep.Waiting;
    private bool trainingComplete;

    private Coroutine typingRoutine;

    // Prevents problems when the Player has multiple colliders.
    private readonly HashSet<Collider> playerColliders = new();

    private void Start()
    {
        HideMessage();
    }

    private void Update()
    {
        if (playerController == null)
        {
            return;
        }

        FacePlayer();

        playerController.UnlockDoubleJump();

        switch (currentStep)
        {
            case TutorialStep.DoubleJump:
                CheckForDoubleJump();
                break;

            case TutorialStep.AirDash:
                CheckForAirDash();
                break;
        }
    }

    private void CheckForDoubleJump()
    {
        if (jumpAction == null)
        {
            return;
        }

        
        
        // A Jump press while already airborne is the second jump.
        if (jumpAction.WasPressedThisFrame() &&
            !playerController.IsGrounded)
        {
            currentStep = TutorialStep.AirDash;
            playerController.UnlockAirDash();
            ShowMessage(airDashMessage);
        }
    }

    private void CheckForAirDash()
    {
        if (dashAction == null)
        {
            return;
        }

        if (dashAction.WasPressedThisFrame() &&
            !playerController.IsGrounded)
        {
            currentStep = TutorialStep.CollectShamrock;

            if (fountainShamrockCollected)
            {
                CompleteTutorial();
            }
            else
            {
                ShowMessage(collectShamrockMessage);
            }
        }
    }

    public void NotifyFountainShamrockCollected()
    {
        if (trainingComplete || completionRoutine != null)
        {
            return;
        }

        completionRoutine = StartCoroutine(
            CompleteTutorialRoutine()
        );
    }

    private IEnumerator CompleteTutorialRoutine()
    {
        fountainShamrockCollected = true;
        currentStep = TutorialStep.Complete;

        // Show the final message before marking the tutorial complete.
        ShowMessage(completeMessage);

        trainingComplete = true;

        // Wait until the letter-by-letter effect finishes.
        yield return new WaitUntil(() => typingRoutine == null);

        // Keep the completed message visible for a few seconds.
        yield return new WaitForSeconds(finalMessageHoldTime);

        HideMessage();

        completionRoutine = null;

        Debug.Log("Movement tutorial completed.");
    }

    private void CompleteTutorial()
    {
        trainingComplete = true;
        currentStep = TutorialStep.Complete;

        ShowMessage(completeMessage);
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (trainingComplete)
        {
            HideMessage();
            return;
        }

        if (controller == null)
        {
            return;
        }

        playerColliders.Add(other);

        // The Player is already inside through another collider.
        if (playerController == controller)
        {
            return;
        }

        PlayerInput playerInput =
            controller.GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError(
                "The Player needs a PlayerInput component.",
                controller
            );

            return;
        }

        playerController = controller;
        player = controller.transform;

        jumpAction = playerInput.actions.FindAction(
            "Jump",
            false
        );

        dashAction = playerInput.actions.FindAction(
            "Dash",
            false
        );

        if (jumpAction == null)
        {
            Debug.LogError(
                "The Player Input Actions do not contain a Jump action.",
                controller
            );
        }

        if (dashAction == null)
        {
            Debug.LogError(
                "The Player Input Actions do not contain a Dash action.",
                controller
            );
        }

        if (trainingComplete)
        {
            currentStep = TutorialStep.Complete;
            ShowMessage(completeMessage);
        }
        else
        {
            currentStep = TutorialStep.DoubleJump;
            ShowMessage(doubleJumpMessage);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null ||
            controller != playerController)
        {
            return;
        }

        playerColliders.Remove(other);

        if (playerColliders.Count > 0)
        {
            return;
        }

        playerController = null;
        player = null;
        jumpAction = null;
        dashAction = null;

        HideMessage();
    }

    private void ShowMessage(string message)
    {
        if (trainingComplete)
        {
            return;
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialText == null)
        {
            return;
        }

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(
            RevealTextRoutine(message)
        );
    }

    private IEnumerator RevealTextRoutine(string message)
    {
        tutorialText.text = message;
        tutorialText.maxVisibleCharacters = 0;

        tutorialText.ForceMeshUpdate();

        int characterCount =
            tutorialText.textInfo.characterCount;

        for (int i = 1; i <= characterCount; i++)
        {
            tutorialText.maxVisibleCharacters = i;

            char character =
                tutorialText.textInfo
                    .characterInfo[i - 1]
                    .character;

            if (!char.IsWhiteSpace(character))
            {
                yield return new WaitForSeconds(
                    letterDelay
                );
            }
        }

        typingRoutine = null;
    }

    private void HideMessage()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (tutorialText != null)
        {
            tutorialText.maxVisibleCharacters =
                int.MaxValue;
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}