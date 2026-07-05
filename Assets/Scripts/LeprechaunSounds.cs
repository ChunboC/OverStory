using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class LeprechaunPlayerAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Footstep Sounds")]
    public AudioClip[] footstepClips;

    [Header("Player Sounds")]
    public AudioClip jumpSound;
    public AudioClip shootSound;

    [Header("Sound Volumes")]
    [Range(0f, 1f)] public float footstepVolume = 0.6f;
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    [Range(0f, 1f)] public float shootVolume = 0.7f;

    [Header("Footstep Settings")]
    public float stepInterval = 0.45f;
    public float minMoveInput = 0.1f;

    [Header("Ground Check")]
    public float groundCheckDistance = 2.0f;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody rb;
    private float stepTimer;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();

        if (playerInput != null)
        {
            moveAction = playerInput.actions["Move"];
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        bool grounded = IsGrounded();
        bool pressingMove = IsPressingMoveInput();

        if (grounded && pressingMove)
        {
            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            // Reset immediately so footsteps stop as soon as player stops or jumps.
            stepTimer = 0f;
        }
    }

    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);
    }

    bool IsPressingMoveInput()
    {
        if (moveAction == null)
        {
            return false;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        return moveInput.magnitude > minMoveInput;
    }

    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip, footstepVolume);
    }

    public void PlayJump()
    {
        PlaySound(jumpSound, jumpVolume);
    }

    public void PlayShoot()
    {
        PlaySound(shootSound, shootVolume);
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clip, volume);
    }
}