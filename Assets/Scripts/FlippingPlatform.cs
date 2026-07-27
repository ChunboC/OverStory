using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlippingPlatform : MonoBehaviour
{
    [Header("Flip Settings")]
    [Tooltip("X = forward/backward flip, Z = side-to-side flip.")]
    [SerializeField] private Vector3 localFlipAxis = Vector3.right;

    [SerializeField] private float flipAngle = 180f;
    [SerializeField] private float flipDuration = 0.75f;
    [SerializeField] private float waitBetweenFlips = 2f;
    [SerializeField] private float startDelay = 0f;

    private Rigidbody platformRigidbody;

    private void Awake()
    {
        platformRigidbody = GetComponent<Rigidbody>();

        platformRigidbody.isKinematic = true;
        platformRigidbody.useGravity = false;
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

    private IEnumerator FlipOnce()
    {
        Quaternion initialRotation = platformRigidbody.rotation;
        Vector3 flipAxis = localFlipAxis.normalized;

        float elapsedTime = 0f;

        while (elapsedTime < flipDuration)
        {
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
                Quaternion.AngleAxis(currentAngle, flipAxis);

            platformRigidbody.MoveRotation(newRotation);

            yield return new WaitForFixedUpdate();
        }

        Quaternion finalRotation =
            initialRotation *
            Quaternion.AngleAxis(flipAngle, flipAxis);

        platformRigidbody.MoveRotation(finalRotation);
    }
}