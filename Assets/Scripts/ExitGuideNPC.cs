using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ExitGuideNPC : MonoBehaviour
{
    [Header("Next Scene")]
    [Tooltip("Enter the scene name without the .unity extension.")]
    [SerializeField] private string nextSceneName;

    [Header("Interaction UI")]
    [SerializeField] private GameObject interactionPanel;
    [SerializeField] private TMP_Text interactionText;

    [Header("Collectible Count")]
    [SerializeField] private float countRefreshInterval = 0.25f;

    private float nextCountRefreshTime;
    private int lastRemainingCount = -1;

    [Header("Ability Requirement Messages")]
    [TextArea]
    [SerializeField]
    private string missingBothAbilitiesMessage =
    "The road ahead is too dangerous without the proper skills. Find Eggy in the village and learn the Double Jump and Air Dash before leaving.";

    [TextArea]
    [SerializeField]
    private string missingDoubleJumpMessage =
        "You have not learned the Double Jump yet. Find Eggy in the village before continuing.";

    [TextArea]
    [SerializeField]
    private string missingAirDashMessage =
        "You still need to learn the Air Dash. Return to Eggy and complete his movement training before leaving.";

    [TextArea]
    [SerializeField]
    private string interactionMessage =
        "I can take you out of here, when you are ready, come talk to me and Press E or the North gamepad button to leave the village. Once you leave, you can't come back.";

    [TextArea]
    [SerializeField]
    private string loadingMessage =
        "The village gate closes behind you as your journey continues...";

    [Header("NPC")]
    [SerializeField] private float turnSpeed = 5f;

    private PlayerController playerController;
    private Transform player;
    private bool isLoading;
    private bool lastDoubleJumpUnlocked;
    private bool lastAirDashUnlocked;
    private bool hasCachedPrompt;

    // Handles Players that have more than one collider.
    private readonly HashSet<Collider> playerColliders = new();

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (playerController == null || isLoading)
        {
            return;
        }

        FacePlayer();

        if (Time.time >= nextCountRefreshTime)
        {
            nextCountRefreshTime =
                Time.time + countRefreshInterval;

            UpdateCollectiblePrompt();
        }

        bool keyboardInteract =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        bool gamepadInteract =
            Gamepad.current != null &&
            Gamepad.current.buttonNorth.wasPressedThisFrame;

        if (keyboardInteract || gamepadInteract)
        {
            LoadNextScene();
        }
    }

    private bool HasRequiredAbilities()
    {
        return playerController != null &&
               playerController.DoubleJumpUnlocked &&
               playerController.AirDashUnlocked;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        

        if (controller == null)
        {
            return;
        }

        playerColliders.Add(other);

        // The Player may have already entered through another collider.
        if (playerController == controller)
        {
            return;
        }

        playerController = controller;
        player = controller.transform;

        hasCachedPrompt = false;
        lastRemainingCount = -1;
        UpdateCollectiblePrompt();

    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null || controller != playerController)
        {
            return;
        }

        playerColliders.Remove(other);

        // Another Player collider is still inside the trigger.
        if (playerColliders.Count > 0)
        {
            return;
        }

        playerController = null;
        player = null;

        HidePrompt();
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

    private void LoadNextScene()
    {
        if (!HasRequiredAbilities())
        {
            Debug.Log(
                "Exit locked: Double Jump and Air Dash are required."
            );

            hasCachedPrompt = false;
            UpdateCollectiblePrompt();
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "No next scene was assigned to the Exit Guide.",
                this
            );

            return;
        }

        isLoading = true;

        if (interactionText != null)
        {
            interactionText.text = loadingMessage;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void ShowPrompt(string message)
    {
        if (interactionText != null)
        {
            interactionText.text = message;
        }

        if (interactionPanel != null)
        {
            interactionPanel.SetActive(true);
        }
    }

    private void UpdateCollectiblePrompt()
    {
        if (playerController == null)
        {
            return;
        }

        bool hasDoubleJump =
            playerController.DoubleJumpUnlocked;

        bool hasAirDash =
            playerController.AirDashUnlocked;

        AreaCollectible[] remainingCollectibles =
            FindObjectsByType<AreaCollectible>(
                FindObjectsSortMode.None
            );

        int remaining = remainingCollectibles.Length;

        if (hasCachedPrompt &&
            remaining == lastRemainingCount &&
            hasDoubleJump == lastDoubleJumpUnlocked &&
            hasAirDash == lastAirDashUnlocked)
        {
            return;
        }

        hasCachedPrompt = true;
        lastRemainingCount = remaining;
        lastDoubleJumpUnlocked = hasDoubleJump;
        lastAirDashUnlocked = hasAirDash;

        // The exit remains locked until both abilities are learned.
        if (!hasDoubleJump && !hasAirDash)
        {
            ShowPrompt(missingBothAbilitiesMessage);
            return;
        }

        if (!hasDoubleJump)
        {
            ShowPrompt(missingDoubleJumpMessage);
            return;
        }

        if (!hasAirDash)
        {
            ShowPrompt(missingAirDashMessage);
            return;
        }

        string message;

        if (remaining == 0)
        {
            message =
                "You found every shamrock in the village! The road ahead is one-way—once you leave, you cannot return. Press E or the North gamepad button to continue.";
        }
        else if (remaining == 1)
        {
            message =
                "One last shamrock is still hidden somewhere in the village. You may leave now, but the gate will close behind you and you cannot return. Press E or the North gamepad button to depart.";
        }
        else
        {
            message =
                $"{remaining} shamrocks are still hidden throughout the village. You may leave without them, but once you cross the gate, you cannot return. Press E or the North gamepad button to depart.";
        }

        ShowPrompt(message);
    }

    private void HidePrompt()
    {
        if (interactionPanel != null)
        {
            interactionPanel.SetActive(false);
        }
    }
}
