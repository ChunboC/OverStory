using UnityEngine;

public class RotateCollectible : MonoBehaviour
{
    [Header("Spin")]
    [Tooltip("Degrees per second. Y-only gives the flat, horizontal spin.")]
    public Vector3 rotationSpeed = new Vector3(0f, 120f, 0f);

    [Tooltip("Spin around world axes, so the shamrock's own stretched orientation can't tip the spin over.")]
    public bool spinInWorldSpace = true;

    [Header("Hover")]
    public float hoverFrequency = 1f;
    public float hoverAmplitude = 0.2f;

    [Tooltip("Offsets the bob so shamrocks placed together don't move in lockstep.")]
    public float hoverPhase = 0f;

    [Header("Moving Platform")]
    [Tooltip("Optional. When set, the shamrock rides this transform instead of holding a fixed point in the world.")]
    public Transform followTarget;

    [Tooltip("World-space offset from followTarget, baked in when the shamrock was placed.")]
    public Vector3 followOffset;

    private Vector3 anchorPosition;

    void Start()
    {
        anchorPosition = transform.position;

        // Placed by hand rather than by the spawner: derive the offset from
        // wherever the shamrock happens to be sitting right now.
        if (followTarget != null && followOffset == Vector3.zero)
        {
            followOffset = transform.position - followTarget.position;
        }
    }

    // LateUpdate, not Update: KinematicPlatform moves in FixedUpdate, so reading
    // the platform afterwards keeps the shamrock from lagging a frame behind it.
    void LateUpdate()
    {
        transform.Rotate(
            rotationSpeed * Time.deltaTime,
            spinInWorldSpace ? Space.World : Space.Self
        );

        Vector3 basePosition = followTarget != null
            ? followTarget.position + followOffset
            : anchorPosition;

        float bob = Mathf.Sin(Time.time * hoverFrequency + hoverPhase) * hoverAmplitude;

        transform.position = basePosition + Vector3.up * bob;
    }
}
