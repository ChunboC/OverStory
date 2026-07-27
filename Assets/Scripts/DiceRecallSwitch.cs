using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DiceRecallSwitch : MonoBehaviour
{
    [Header("Dice Platform")]
    [SerializeField] private MovingDicePlatform dicePlatform;

    [Header("Input")]
    [Tooltip("Optional Interact action. The script also supports E and gamepad North.")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Prompt")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;

    [TextArea]
    [SerializeField]
    private string promptMessage =
        "Press E / North Button to call the cube back";

    [Header("Switch Animation")]
    [Tooltip("The visible button or lever that moves when activated.")]
    [SerializeField] private Transform switchVisual;

    [SerializeField]
    private Vector3 pressedLocalOffset =
        new Vector3(0f, -0.15f, 0f);

    [SerializeField] private float pressDuration = 0.15f;
    [SerializeField] private float holdDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchSound;

    [Range(0f, 1f)]
    [SerializeField] private float switchVolume = 0.8f;

    [Header("Settings")]
    [SerializeField] private float activationCooldown = 1f;

    private Vector3 switchStartingPosition;
    private bool playerInRange;
    private bool activationLocked;
    private int playerColliderCount;
    private Coroutine switchRoutine;

    private void Awake()
    {
        if (switchVisual != null)
        {
            switchStartingPosition =
                switchVisual.localPosition;
        }

        HidePrompt();
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.Disable();
        }
    }

    private void Update()
    {
        if (!playerInRange || activationLocked)
        {
            return;
        }

        bool inputActionPressed =
            interactAction != null &&
            interactAction.action.WasPressedThisFrame();

        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        bool controllerPressed =
            Gamepad.current != null &&
            Gamepad.current.buttonNorth.wasPressedThisFrame;

        if (inputActionPressed ||
            keyboardPressed ||
            controllerPressed)
        {
            ActivateSwitch();
        }
    }

    private void ActivateSwitch()
    {
        if (dicePlatform == null)
        {
            Debug.LogWarning(
                "The recall switch does not have a dice platform assigned.",
                this
            );

            return;
        }

        dicePlatform.RecallPlatform();

        if (audioSource != null &&
            switchSound != null)
        {
            audioSource.PlayOneShot(
                switchSound,
                switchVolume
            );
        }

        if (switchRoutine != null)
        {
            StopCoroutine(switchRoutine);
        }

        switchRoutine =
            StartCoroutine(AnimateSwitch());

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator AnimateSwitch()
    {
        if (switchVisual == null)
        {
            yield break;
        }

        Vector3 pressedPosition =
            switchStartingPosition +
            pressedLocalOffset;

        float elapsedTime = 0f;

        while (elapsedTime < pressDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / pressDuration
            );

            switchVisual.localPosition =
                Vector3.Lerp(
                    switchStartingPosition,
                    pressedPosition,
                    progress
                );

            yield return null;
        }

        switchVisual.localPosition =
            pressedPosition;

        yield return new WaitForSeconds(
            holdDuration
        );

        elapsedTime = 0f;

        while (elapsedTime < pressDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / pressDuration
            );

            switchVisual.localPosition =
                Vector3.Lerp(
                    pressedPosition,
                    switchStartingPosition,
                    progress
                );

            yield return null;
        }

        switchVisual.localPosition =
            switchStartingPosition;

        switchRoutine = null;
    }

    private IEnumerator CooldownRoutine()
    {
        activationLocked = true;

        yield return new WaitForSeconds(
            activationCooldown
        );

        activationLocked = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.transform.root.CompareTag("Player"))
        {
            return;
        }

        playerColliderCount++;
        playerInRange = true;

        ShowPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.transform.root.CompareTag("Player"))
        {
            return;
        }

        playerColliderCount =
            Mathf.Max(0, playerColliderCount - 1);

        if (playerColliderCount == 0)
        {
            playerInRange = false;
            HidePrompt();
        }
    }

    private void ShowPrompt()
    {
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }

        if (promptObject != null)
        {
            promptObject.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
    }
}