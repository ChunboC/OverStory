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

    [Header("Player Carry")]
    [Tooltip("How much additional movement is applied to the player.")]
    [Range(0f, 1f)]
    [SerializeField] private float carryAssist = 0.5f;

    [Header("Dice Rotation")]
    [SerializeField] private float rollAngle = 90f;
    [SerializeField] private float rollDuration = 1.25f;
    [SerializeField] private float waitBetweenRolls = 0.75f;

    [Tooltip("Randomly rotate around world X or world Z.")]
    [SerializeField] private bool randomAxis = true;

    [Tooltip("Randomly rotate in either direction.")]
    [SerializeField] private bool randomDirection = true;

    [Tooltip("Stop rotating after reaching the destination.")]
    [SerializeField] private bool stopRotatingAtEnd = true;

    [Header("Rotation Center")]
    [Tooltip("Assign the cube's solid collider, not its trigger.")]
    [SerializeField] private Collider platformCollider;

    private Rigidbody platformRigidbody;
    private Rigidbody passengerRigidbody;

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

    private bool hasStarted;
    private bool reachedEnd;
    private bool isRolling;
    private bool completed;
    private bool playerInsideDetector;

    private Vector3 originalRigidbodyPosition;
    private Quaternion originalRigidbodyRotation;
    private Vector3 originalCenter;

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
            // Saves the solid collider's center relative to the Rigidbody.
            localCenterOffset =
                Quaternion.Inverse(platformRigidbody.rotation) *
                (
                    platformCollider.bounds.center -
                    platformRigidbody.position
                );
        }
        else
        {
            localCenterOffset = Vector3.zero;

            Debug.LogWarning(
                "MovingDicePlatform needs the cube's solid collider.",
                this
            );
        }

        originalRigidbodyPosition =
            platformRigidbody.position;

        originalRigidbodyRotation =
            platformRigidbody.rotation;

        originalCenter =
            originalRigidbodyPosition +
            originalRigidbodyRotation *
            localCenterOffset;
    }

    private void FixedUpdate()
    {
        if (!hasStarted || completed)
        {
            return;
        }

        Vector3 previousCenter = currentCenter;

        UpdateHorizontalMovement();
        UpdateDiceRotation();

        // Only use the actual horizontal travel for player assistance.
        Vector3 horizontalMovement =
            currentCenter - previousCenter;

        horizontalMovement.y = 0f;

        // Keeps the platform rotating around the collider center.
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

        // Allow the current roll to finish before stopping.
        if (reachedEnd &&
            stopRotatingAtEnd &&
            !isRolling)
        {
            completed = true;
        }
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

    private void BeginMovement()
    {
        Vector3 horizontalDirection = moveDirection;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning(
                "MovingDicePlatform needs a horizontal direction.",
                this
            );

            return;
        }

        horizontalDirection.Normalize();

        startingCenter =
            platformRigidbody.position +
            platformRigidbody.rotation *
            localCenterOffset;

        endingCenter =
            startingCenter +
            horizontalDirection *
            Mathf.Abs(moveDistance);

        currentCenter = startingCenter;
        currentRotation = platformRigidbody.rotation;

        distanceMoved = 0f;
        rollElapsed = 0f;
        waitElapsed = 0f;

        reachedEnd = false;
        completed = false;
        hasStarted = true;

        // Start rotating immediately.
        StartNextRoll();

        Debug.Log("Dice platform activated.", this);
    }

    private void UpdateHorizontalMovement()
    {
        if (reachedEnd)
        {
            currentCenter = endingCenter;
            return;
        }

        float totalDistance =
            Mathf.Abs(moveDistance);

        distanceMoved = Mathf.MoveTowards(
            distanceMoved,
            totalDistance,
            moveSpeed * Time.fixedDeltaTime
        );

        float progress = totalDistance <= 0f
            ? 1f
            : distanceMoved / totalDistance;

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
        Vector3 worldAxis =
            ChooseWorldRotationAxis();

        float direction = 1f;

        if (randomDirection)
        {
            direction =
                Random.value < 0.5f
                    ? -1f
                    : 1f;
        }

        rollStartRotation = currentRotation;

        // Uses world X or world Z only.
        // It never spins horizontally around world Y.
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

    private void CarryPassenger(
        Vector3 platformMovement)
    {
        if (!playerInsideDetector ||
            passengerRigidbody == null)
        {
            return;
        }

        passengerRigidbody.MovePosition(
            passengerRigidbody.position +
            platformMovement
        );
    }

    private Rigidbody GetPlayerRigidbody(
        Collider other)
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
                other.transform.root
                    .GetComponentInChildren<Rigidbody>();
        }

        return detectedRigidbody;
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

        Vector3 destination =
            transform.position +
            direction *
            Mathf.Abs(moveDistance);

        Gizmos.DrawLine(
            transform.position,
            destination
        );

        Gizmos.DrawWireSphere(
            destination,
            0.4f
        );
    }

    public void RecallPlatform()
    {
        hasStarted = false;
        reachedEnd = false;
        isRolling = false;
        completed = false;

        playerInsideDetector = false;
        passengerRigidbody = null;

        distanceMoved = 0f;
        rollElapsed = 0f;
        waitElapsed = 0f;

        startingCenter = originalCenter;
        endingCenter = originalCenter;
        currentCenter = originalCenter;

        currentRotation = originalRigidbodyRotation;
        rollStartRotation = originalRigidbodyRotation;
        rollTargetRotation = originalRigidbodyRotation;

        platformRigidbody.position =
            originalRigidbodyPosition;

        platformRigidbody.rotation =
            originalRigidbodyRotation;

        platformRigidbody.linearVelocity = Vector3.zero;
        platformRigidbody.angularVelocity = Vector3.zero;

        Debug.Log("Dice platform recalled.", this);
    }
}