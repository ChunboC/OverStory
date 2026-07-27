using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlippingPlatform : MonoBehaviour
{
    [Header("Flip Settings")]
    [Tooltip("X = forward/backward flip. Z = side-to-side flip.")]
    [SerializeField] private Vector3 localFlipAxis = Vector3.right;

    [SerializeField] private float flipAngle = 180f;
    [SerializeField] private float flipDuration = 0.75f;
    [SerializeField] private float waitBetweenFlips = 2f;
    [SerializeField] private float startDelay = 0f;

    [Header("Floating Settings")]
    [Tooltip("Maximum movement from the starting position.")]
    [SerializeField]
    private Vector3 floatRange =
        new Vector3(0f, 0.5f, 0f);

    [SerializeField] private float floatSpeed = 1f;

    [Tooltip("Give each platform a different value so they do not move together.")]
    [SerializeField] private float floatPhase = 0f;

    private Rigidbody platformRigidbody;
    private Vector3 startingPosition;

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();

        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity = false;
        platformRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        platformRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        startingPosition = platformRigidbody.position;
    }

    private IEnumerator Start()
    {
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        while (true)
        {
            yield return new WaitForSeconds(waitBetweenFlips);
            yield return FlipOnce();
        }
    }

    private void FixedUpdate()
    {
        FloatPlatform();
    }

    private void FloatPlatform()
    {
        float movementTime =
            Time.fixedTime * floatSpeed + floatPhase;

        Vector3 offset = new Vector3(
            Mathf.Cos(movementTime) * floatRange.x,
            Mathf.Sin(movementTime) * floatRange.y,
            Mathf.Sin(movementTime) * floatRange.z
        );

        Vector3 targetPosition =
            startingPosition + offset;

        platformRigidbody.MovePosition(targetPosition);
    }

    private IEnumerator FlipOnce()
    {
        if (localFlipAxis.sqrMagnitude < 0.001f)
        {
            yield break;
        }

        Quaternion initialRotation =
            platformRigidbody.rotation;

        Vector3 flipAxis =
            localFlipAxis.normalized;

        float elapsedTime = 0f;

        while (elapsedTime < flipDuration)
        {
            yield return new WaitForFixedUpdate();

            elapsedTime += Time.fixedDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / flipDuration
            );

            float smoothProgress = Mathf.SmoothStep(
                0f,
                1f,
                progress
            );

            float currentAngle = Mathf.Lerp(
                0f,
                flipAngle,
                smoothProgress
            );

            Quaternion newRotation =
                initialRotation *
                Quaternion.AngleAxis(
                    currentAngle,
                    flipAxis
                );

            platformRigidbody.MoveRotation(
                newRotation
            );
        }

        Quaternion finalRotation =
            initialRotation *
            Quaternion.AngleAxis(
                flipAngle,
                flipAxis
            );

        platformRigidbody.MoveRotation(
            finalRotation
        );
    }
}