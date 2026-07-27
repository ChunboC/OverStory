using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCannon : MonoBehaviour
{
    [Header("Cannon Points")]
    [SerializeField] private Transform seatPoint;
    [SerializeField] private Transform launchPoint;

    [Header("Landing Targets")]
    [SerializeField] private Transform[] landingTargets;
    [SerializeField] private GameObject selectedTargetIndicator;
    [SerializeField] private float indicatorHeightOffset = 0.05f;

    [Header("Cameras")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera cannonCamera;

    [Header("Launch Settings")]
    [SerializeField] private float arcHeight = 6f;
    [SerializeField] private float inputDelay = 0.35f;
    [SerializeField] private float landingTimeout = 8f;

    [Header("Cannon Effects")]
    [SerializeField] private ParticleSystem cannonSmoke;

    [Header("Cannon Audio")]
    [SerializeField] private AudioSource cannonAudioSource;
    [SerializeField] private AudioClip cannonShootSound;

    [Range(0f, 1f)]
    [SerializeField] private float cannonShootVolume = 0.9f;

    [Range(0.5f, 2f)]
    [SerializeField] private float cannonShootPitch = 1f;

    private PlayerController playerController;
    private Rigidbody playerRigidbody;
    private PlayerInput playerInput;

    private InputAction moveAction;
    private InputAction jumpAction;

    private int selectedTarget;
    private bool cannonActive;
    private bool horizontalInputHeld;
    private float inputReadyTime;

    private void Start()
    {
        if (cannonCamera != null)
        {
            cannonCamera.gameObject.SetActive(false);
        }

        UpdateTargetIndicator();
    }

    private void Update()
    {
        if (!cannonActive || Time.time < inputReadyTime)
        {
            return;
        }

        HandleTargetSelection();

        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            LaunchPlayer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cannonActive)
        {
            return;
        }

        PlayerController controller =
            other.GetComponentInParent<PlayerController>();

        if (controller == null)
        {
            return;
        }

        EnterCannon(controller);
    }

    private void EnterCannon(PlayerController controller)
    {
        playerController = controller;
        playerRigidbody = controller.GetComponent<Rigidbody>();
        playerInput = controller.GetComponent<PlayerInput>();

        if (playerRigidbody == null || playerInput == null)
        {
            Debug.LogError(
                "The Player needs a Rigidbody and PlayerInput component.",
                controller
            );
            return;
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];

        if (moveAction == null || jumpAction == null)
        {
            Debug.LogError(
                "The Player Input Actions must contain Move and Jump actions.",
                controller
            );
            return;
        }

        cannonActive = true;
        selectedTarget = 0;
        horizontalInputHeld = false;
        inputReadyTime = Time.time + inputDelay;

        playerController.SetControlsLocked(true);

        playerRigidbody.linearVelocity = Vector3.zero;
        playerRigidbody.angularVelocity = Vector3.zero;
        playerRigidbody.isKinematic = true;

        playerController.transform.SetPositionAndRotation(
            seatPoint.position,
            seatPoint.rotation
        );

        playerCamera.gameObject.SetActive(false);
        cannonCamera.gameObject.SetActive(true);

        UpdateTargetIndicator();
    }

    private void HandleTargetSelection()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float horizontalInput = moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.6f &&
            !horizontalInputHeld)
        {
            int direction = horizontalInput > 0f ? 1 : -1;

            selectedTarget += direction;

            if (selectedTarget >= landingTargets.Length)
            {
                selectedTarget = 0;
            }
            else if (selectedTarget < 0)
            {
                selectedTarget = landingTargets.Length - 1;
            }

            horizontalInputHeld = true;
            UpdateTargetIndicator();
        }
        else if (Mathf.Abs(horizontalInput) < 0.25f)
        {
            horizontalInputHeld = false;
        }
    }

    private void LaunchPlayer()
    {
        if (landingTargets == null || landingTargets.Length == 0)
        {
            Debug.LogError("The cannon has no landing targets.", this);
            return;
        }

        Transform selectedLandingTarget =
            landingTargets[selectedTarget];

        cannonCamera.gameObject.SetActive(false);
        playerCamera.gameObject.SetActive(true);

        playerController.transform.SetPositionAndRotation(
            launchPoint.position,
            launchPoint.rotation
        );

        playerRigidbody.isKinematic = false;

        Vector3 launchVelocity = CalculateLaunchVelocity(
            launchPoint.position,
            selectedLandingTarget.position,
            arcHeight
        );

        if (cannonSmoke != null)
        {
            cannonSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            cannonSmoke.Play();
        }

        PlayCannonSound();

        playerRigidbody.linearVelocity = launchVelocity;

        cannonActive = false;

        StartCoroutine(UnlockPlayerAfterLanding());
    }

    private void PlayCannonSound()
    {
        if (cannonAudioSource == null || cannonShootSound == null)
        {
            return;
        }

        cannonAudioSource.Stop();
        cannonAudioSource.pitch = cannonShootPitch;

        cannonAudioSource.PlayOneShot(
            cannonShootSound,
            cannonShootVolume
        );
    }

    private Vector3 CalculateLaunchVelocity(
        Vector3 start,
        Vector3 target,
        float additionalArcHeight)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);

        float peakY =
            Mathf.Max(start.y, target.y) + additionalArcHeight;

        float verticalSpeed = Mathf.Sqrt(
            2f * gravity * (peakY - start.y)
        );

        float timeUp = verticalSpeed / gravity;

        float timeDown = Mathf.Sqrt(
            2f * (peakY - target.y) / gravity
        );

        float totalTime = timeUp + timeDown;

        Vector3 horizontalDisplacement = target - start;
        horizontalDisplacement.y = 0f;

        Vector3 horizontalVelocity =
            horizontalDisplacement / totalTime;

        return horizontalVelocity +
               Vector3.up * verticalSpeed;
    }

    private IEnumerator UnlockPlayerAfterLanding()
    {
        // Prevent the starting ground check from ending the flight early.
        yield return new WaitForSeconds(0.25f);

        float timer = landingTimeout;

        while (!playerController.IsGrounded && timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        playerController.SetControlsLocked(false);
    }

    private void UpdateTargetIndicator()
    {
        if (selectedTargetIndicator == null ||
            landingTargets == null ||
            landingTargets.Length == 0)
        {
            return;
        }

        selectedTargetIndicator.SetActive(cannonActive);

        if (!cannonActive)
        {
            return;
        }

        Vector3 indicatorPosition =
            landingTargets[selectedTarget].position;

        indicatorPosition.y += indicatorHeightOffset;

        selectedTargetIndicator.transform.position =
            indicatorPosition;
    }
}