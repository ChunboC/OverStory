using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Rigidbody))]

public class MovingDicePlatform : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [Tooltip("X = left/right, Z = forward/backward. Y is ignored.")]
    [SerializeField] private Vector3 moveDirection = Vector3.right;

    [SerializeField] private float moveDistance = 12f;
    [SerializeField] private float moveSpeed = 1.5f;

    [Header("Dice Rotation")]
    [SerializeField] private float rollAngle = 90f;
    [SerializeField] private float rollDuration = 1.25f;
    [SerializeField] private float waitBetweenRolls = 0.75f;

    [Tooltip("Randomly rotate around world X or world Z.")]
    [SerializeField] private bool randomAxis = true;

    [Tooltip("Randomly choose the rotation direction.")]
    [SerializeField] private bool randomDirection = true;

    [Tooltip("Stop rotating after reaching the destination.")]
    [SerializeField] private bool stopRotatingAtEnd = true;

    [Header("Rotation Center")]
    [Tooltip("Assign the cube's solid collider, not the trigger.")]
    [SerializeField] private Collider platformCollider;

    [Header("Player Carry")]
    [Range(0f, 1f)]
    [SerializeField] private float carryAssist = 0.5f;

    private Rigidbody platformRigidbody;
    private Rigidbody passengerRigidbody;
    private bool playerInsideDetector;

    private Vector3 localCenterOffset;
    private Vector3 startingCenter;
    private Vector3 endingCenter;
    private Vector3 currentCenter;

    private Quaternion currentRotation;
    private Quaternion rollStartRotation;
    private Quaternion rollTargetRotation;

    private float distanceMoved;
    private float rollElapsed;
    private float waitElapsed;

    private bool playerStandingOnPlatform;
    private bool hasStarted;
    private bool reachedEnd;
    private bool isRolling;
    private bool completed;

    private int patternIndex;

    private readonly Vector3[] rotationPattern =
    {
        Vector3.right,
        Vector3.forward,
        Vector3.right,
        Vector3.forward
    };

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();

        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity = false;
        platformRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        platformRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        FindPlatformCollider();

        currentRotation = platformRigidbody.rotation;

        if (platformCollider != null)
        {
            // Saves the collider center relative to the Rigidbody.
            localCenterOffset =
                Quaternion.Inverse(platformRigidbody.rotation) *
                (platformCollider.bounds.center -
                 platformRigidbody.position);
        }
        else
        {
            localCenterOffset = Vector3.zero;

            Debug.LogWarning(
                "MovingDicePlatform needs the cube's solid collider."
            );
        }
    }

    private void FixedUpdate()
    {
        if (!hasStarted || completed)
        {
            return;
        }

        // Save the platform's travel center before updating it.
        Vector3 previousCenter = currentCenter;

        UpdateHorizontalMovement();
        UpdateDiceRotation();

        // Only use the platform's actual horizontal travel.
        // Do not include movement caused by rotating around an offset pivot.
        Vector3 horizontalMovement =
            currentCenter - previousCenter;

        horizontalMovement.y = 0f;

        Vector3 newPlatformPosition =
            currentCenter -
            currentRotation * localCenterOffset;

        platformRigidbody.MovePosition(
            newPlatformPosition
        );

        platformRigidbody.MoveRotation(
            currentRotation
        );

        CarryPassenger(
            horizontalMovement * carryAssist
        );

        if (reachedEnd &&
            stopRotatingAtEnd &&
            !isRolling)
        {
            completed = true;
        }
    }

    private void CarryPassenger(Vector3 platformMovement)
    {
        if (!playerInsideDetector ||
            passengerRigidbody == null)
        {
            return;
        }

        // Carry the player with the platform's translation.
        // The cube's physical rotation still affects the player naturally.
        passengerRigidbody.MovePosition(
            passengerRigidbody.position +
            platformMovement
        );
    }

    private void CarryPlayer(
    Vector3 oldPlatformPosition,
    Quaternion oldPlatformRotation,
    Vector3 newPlatformPosition,
    Quaternion newPlatformRotation)
    {
        if (!playerStandingOnPlatform ||
            passengerRigidbody == null)
        {
            return;
        }

        // Find the player's position relative to the old platform pose.
        Vector3 relativePlayerPosition =
            Quaternion.Inverse(oldPlatformRotation) *
            (passengerRigidbody.position -
             oldPlatformPosition);

        // Apply both the cube's movement and rotation to the player.
        Vector3 targetPlayerPosition =
            newPlatformPosition +
            newPlatformRotation *
            relativePlayerPosition;

        Vector3 movementDifference =
            targetPlayerPosition -
            passengerRigidbody.position;

        passengerRigidbody.MovePosition(
            passengerRigidbody.position +
            movementDifference
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody detectedRigidbody =
            GetPlayerRigidbody(other);

        if (detectedRigidbody == null)
        {
            return;
        }

        passengerRigidbody = detectedRigidbody;
        playerInsideDetector = true;

        if (!hasStarted)
        {
            BeginMovement();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody detectedRigidbody =
            GetPlayerRigidbody(other);

        if (detectedRigidbody == null)
        {
            return;
        }

        passengerRigidbody = detectedRigidbody;
        playerInsideDetector = true;
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody detectedRigidbody =
            GetPlayerRigidbody(other);

        if (detectedRigidbody == null)
        {
            return;
        }

        if (detectedRigidbody == passengerRigidbody)
        {
            playerInsideDetector = false;
            passengerRigidbody = null;
        }
    }

    private Rigidbody GetPlayerRigidbody(Collider other)
    {
        if (!other.transform.root.CompareTag("Player"))
        {
            return null;
        }

        Rigidbody detectedRigidbody =
            other.GetComponentInParent<Rigidbody>();

        if (detectedRigidbody == null)
        {
            detectedRigidbody =
                other.transform.root.GetComponentInChildren<Rigidbody>();
        }

        return detectedRigidbody;
    }

    private void BeginMovement()
    {
        Vector3 horizontalDirection = moveDirection;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                "MovingDicePlatform needs a horizontal direction."
            );

            return;
        }

        horizontalDirection.Normalize();

        startingCenter =
            platformRigidbody.position +
            platformRigidbody.rotation * localCenterOffset;

        endingCenter =
            startingCenter +
            horizontalDirection * moveDistance;

        currentCenter = startingCenter;
        currentRotation = platformRigidbody.rotation;

        distanceMoved = 0f;
        reachedEnd = false;
        completed = false;
        hasStarted = true;

        // Begin the first rotation immediately.
        StartNextRoll();

        Debug.Log("Dice platform activated.");
    }

    private void UpdateHorizontalMovement()
    {
        if (reachedEnd)
        {
            currentCenter = endingCenter;
            return;
        }

        distanceMoved = Mathf.MoveTowards(
            distanceMoved,
            moveDistance,
            moveSpeed * Time.fixedDeltaTime
        );

        float progress = moveDistance <= 0f
            ? 1f
            : distanceMoved / moveDistance;

        currentCenter = Vector3.Lerp(
            startingCenter,
            endingCenter,
            progress
        );

        if (progress >= 1f)
        {
            reachedEnd = true;
            currentCenter = endingCenter;
        }
    }

    private void UpdateDiceRotation()
    {
        if (isRolling)
        {
            rollElapsed += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(
                rollElapsed / rollDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            currentRotation = Quaternion.Slerp(
                rollStartRotation,
                rollTargetRotation,
                smoothProgress
            );

            if (progress >= 1f)
            {
                currentRotation = rollTargetRotation;
                isRolling = false;
                waitElapsed = 0f;
            }

            return;
        }

        // Do not begin another rotation after reaching the end.
        if (reachedEnd && stopRotatingAtEnd)
        {
            return;
        }

        waitElapsed += Time.fixedDeltaTime;

        if (waitElapsed >= waitBetweenRolls)
        {
            StartNextRoll();
        }
    }

    private void StartNextRoll()
    {
        Vector3 worldAxis = ChooseWorldRotationAxis();

        float direction = 1f;

        if (randomDirection)
        {
            direction = Random.value < 0.5f
                ? -1f
                : 1f;
        }

        rollStartRotation = currentRotation;

        // World X and Z only. Never rotate around world Y.
        rollTargetRotation =
            Quaternion.AngleAxis(
                rollAngle * direction,
                worldAxis
            ) *
            rollStartRotation;

        rollElapsed = 0f;
        waitElapsed = 0f;
        isRolling = true;
    }

    private Vector3 ChooseWorldRotationAxis()
    {
        if (randomAxis)
        {
            return Random.value < 0.5f
                ? Vector3.right
                : Vector3.forward;
        }

        Vector3 selectedAxis =
            rotationPattern[patternIndex];

        patternIndex++;

        if (patternIndex >= rotationPattern.Length)
        {
            patternIndex = 0;
        }

        return selectedAxis;
    }

    private void FindPlatformCollider()
    {
        if (platformCollider != null)
        {
            return;
        }

        Collider[] colliders =
            GetComponentsInChildren<Collider>();

        foreach (Collider currentCollider in colliders)
        {
            if (!currentCollider.isTrigger)
            {
                platformCollider = currentCollider;
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 direction = moveDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        direction.Normalize();

        Gizmos.DrawLine(
            transform.position,
            transform.position + direction * moveDistance
        );

        Gizmos.DrawWireSphere(
            transform.position + direction * moveDistance,
            0.4f
        );
    }
}