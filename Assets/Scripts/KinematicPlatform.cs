using UnityEngine;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody))]
public class KinematicPlatform : MonoBehaviour
{
    [Header("Movement Offsets (relative to parent at startup)")]
    public Vector3 pointA = new Vector3(0f, 0f, -5f);
    public Vector3 pointB = new Vector3(0f, 0f, 5f);

    [Header("Movement Settings")]
    public float speed = 2f;
    public bool startMovingToB = true;
    public float arrivalThreshold = 0.05f;

    [Header("Pause Settings")]
    public float pauseDuration = 0.5f;

    private Rigidbody _rb;
    private Vector3 _origin;
    private bool _movingToB;
    private float _pauseTimer;
    private bool _paused;
    public Vector3 CurrentVelocity { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _origin = transform.parent != null
            ? transform.parent.position
            : transform.position;

        _movingToB = startMovingToB;
    }

    void FixedUpdate()
    {
        if (_paused)
        {
            CurrentVelocity = Vector3.zero;

            _pauseTimer -= Time.fixedDeltaTime;

            if (_pauseTimer <= 0f)
            {
                _paused = false;
            }

            return;
        }

        Vector3 target = _origin + (_movingToB ? pointB : pointA);

        Vector3 currentPosition = _rb.position;

        Vector3 nextPosition = Vector3.MoveTowards(
            _rb.position,
            target,
            speed * Time.fixedDeltaTime
        );

        CurrentVelocity = (nextPosition - currentPosition) / Time.fixedDeltaTime;

        _rb.MovePosition(nextPosition);

        if (Vector3.Distance(nextPosition, target) <= arrivalThreshold)
        {
            _rb.MovePosition(target);

            _movingToB = !_movingToB;

            _paused = true;
            _pauseTimer = pauseDuration;
        }
    }
}