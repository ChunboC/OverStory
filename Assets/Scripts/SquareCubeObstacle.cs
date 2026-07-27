using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SquareCubeObstacle : MonoBehaviour
{
    public enum TravelDirection
    {
        ForwardUpBackwardDown,
        Reverse
    }

    [Header("Square Path Center")]
    [Tooltip("Place this at the center of the square path.")]
    [SerializeField] private Transform pathCenter;

    [Header("Cube Center")]
    [Tooltip("Assign the cube's solid collider, not a trigger.")]
    [SerializeField] private Collider platformCollider;

    [Header("Square Size")]
    [Tooltip("Distance from the center toward positive and negative Z.")]
    [SerializeField] private float zHalfDistance = 5f;

    [Tooltip("Distance from the center toward positive and negative Y.")]
    [SerializeField] private float yHalfDistance = 4f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [SerializeField]
    private TravelDirection direction =
        TravelDirection.ForwardUpBackwardDown;

    [Tooltip("Starting corner: 0, 1, 2, or 3.")]
    [Range(0, 3)]
    [SerializeField] private int startingCorner;

    [Tooltip("Keep the box facing the same direction.")]
    [SerializeField] private bool keepRotationFixed = true;

    [SerializeField] private float cornerReachedDistance = 0.02f;

    private Rigidbody cubeRigidbody;
    private Quaternion startingRotation;

    private readonly Vector3[] pathPoints = new Vector3[4];

    private Vector3 localCenterOffset;
    private Vector3 currentCenter;

    private int currentCorner;
    private int targetCorner;
    private float fixedXPosition;

    private void Awake()
    {
        cubeRigidbody = GetComponent<Rigidbody>();

        cubeRigidbody.isKinematic = true;
        cubeRigidbody.useGravity = false;
        cubeRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        cubeRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        startingRotation = cubeRigidbody.rotation;

        FindPlatformCollider();

        if (platformCollider != null)
        {
            // Store the collider's center relative to the Rigidbody pivot.
            localCenterOffset =
                Quaternion.Inverse(cubeRigidbody.rotation) *
                (
                    platformCollider.bounds.center -
                    cubeRigidbody.position
                );

            fixedXPosition =
                platformCollider.bounds.center.x;
        }
        else
        {
            localCenterOffset = Vector3.zero;
            fixedXPosition = cubeRigidbody.position.x;

            Debug.LogWarning(
                $"SquareCubeObstacle on '{name}' could not find a solid collider.",
                this
            );
        }
    }

    private void Start()
    {
        if (pathCenter == null)
        {
            Debug.LogError(
                $"SquareCubeObstacle on '{name}' needs a Path Center.",
                this
            );

            enabled = false;
            return;
        }

        BuildSquarePath();

        currentCorner = startingCorner;
        targetCorner = GetNextCorner(currentCorner);

        // The path points now represent the cube's center.
        currentCenter = pathPoints[currentCorner];

        cubeRigidbody.position =
            GetRigidbodyPositionFromCenter(currentCenter);
    }

    private void FixedUpdate()
    {
        Vector3 targetCenter =
            pathPoints[targetCorner];

        currentCenter = Vector3.MoveTowards(
            currentCenter,
            targetCenter,
            moveSpeed * Time.fixedDeltaTime
        );

        // Move the Rigidbody so the collider center follows the path.
        Vector3 targetRigidbodyPosition =
            GetRigidbodyPositionFromCenter(currentCenter);

        cubeRigidbody.MovePosition(
            targetRigidbodyPosition
        );

        if (keepRotationFixed)
        {
            cubeRigidbody.MoveRotation(startingRotation);
        }

        if (Vector3.Distance(
                currentCenter,
                targetCenter
            ) <= cornerReachedDistance)
        {
            currentCenter = targetCenter;

            cubeRigidbody.MovePosition(
                GetRigidbodyPositionFromCenter(currentCenter)
            );

            currentCorner = targetCorner;
            targetCorner = GetNextCorner(currentCorner);
        }
    }

    private Vector3 GetRigidbodyPositionFromCenter(
        Vector3 desiredCenter)
    {
        Quaternion rotation = keepRotationFixed
            ? startingRotation
            : cubeRigidbody.rotation;

        return desiredCenter -
               rotation * localCenterOffset;
    }

    private void BuildSquarePath()
    {
        float centerY = pathCenter.position.y;
        float centerZ = pathCenter.position.z;

        // Corner 0: bottom and behind.
        pathPoints[0] = new Vector3(
            fixedXPosition,
            centerY - yHalfDistance,
            centerZ - zHalfDistance
        );

        // Corner 1: bottom and forward.
        pathPoints[1] = new Vector3(
            fixedXPosition,
            centerY - yHalfDistance,
            centerZ + zHalfDistance
        );

        // Corner 2: top and forward.
        pathPoints[2] = new Vector3(
            fixedXPosition,
            centerY + yHalfDistance,
            centerZ + zHalfDistance
        );

        // Corner 3: top and behind.
        pathPoints[3] = new Vector3(
            fixedXPosition,
            centerY + yHalfDistance,
            centerZ - zHalfDistance
        );
    }

    private int GetNextCorner(int corner)
    {
        if (direction ==
            TravelDirection.ForwardUpBackwardDown)
        {
            return (corner + 1) % pathPoints.Length;
        }

        return (corner - 1 + pathPoints.Length) %
               pathPoints.Length;
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

    public void ReverseDirection()
    {
        direction =
            direction ==
            TravelDirection.ForwardUpBackwardDown
                ? TravelDirection.Reverse
                : TravelDirection.ForwardUpBackwardDown;

        targetCorner = GetNextCorner(currentCorner);
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (pathCenter == null)
        {
            return;
        }

        float previewX = transform.position.x;

        if (platformCollider != null)
        {
            previewX = platformCollider.bounds.center.x;
        }

        Vector3 bottomBack = new Vector3(
            previewX,
            pathCenter.position.y - yHalfDistance,
            pathCenter.position.z - zHalfDistance
        );

        Vector3 bottomFront = new Vector3(
            previewX,
            pathCenter.position.y - yHalfDistance,
            pathCenter.position.z + zHalfDistance
        );

        Vector3 topFront = new Vector3(
            previewX,
            pathCenter.position.y + yHalfDistance,
            pathCenter.position.z + zHalfDistance
        );

        Vector3 topBack = new Vector3(
            previewX,
            pathCenter.position.y + yHalfDistance,
            pathCenter.position.z - zHalfDistance
        );

        Gizmos.DrawLine(bottomBack, bottomFront);
        Gizmos.DrawLine(bottomFront, topFront);
        Gizmos.DrawLine(topFront, topBack);
        Gizmos.DrawLine(topBack, bottomBack);

        Gizmos.DrawWireSphere(bottomBack, 0.2f);
        Gizmos.DrawWireSphere(bottomFront, 0.2f);
        Gizmos.DrawWireSphere(topFront, 0.2f);
        Gizmos.DrawWireSphere(topBack, 0.2f);
    }
}