using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class LeprechaunPlayerAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;
    public AudioSource dashAudioSource;

    [Header("Footstep Sounds")]
    public AudioClip[] footstepClips;

    [Header("Player Sounds")]
    public AudioClip jumpSound;
    public AudioClip shootSound;
    public AudioClip dashSound;

    [Header("Sound Volumes")]
    [Range(0f, 1f)] public float footstepVolume = 0.6f;
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    [Range(0f, 1f)] public float shootVolume = 0.7f;
    [Range(0f, 1f)] public float dashVolume = 1f;

    [Header("Sound Pitch")]
    [Range(0.5f, 2f)]
    public float dashPitch = 1.25f;

    [Header("Footstep Settings")]
    public float stepInterval = 0.45f;
    public float minMoveInput = 0.1f;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private Rigidbody rb;
    private float stepTimer;
    private PlayerController playerController;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (dashAudioSource != null)
        {
            dashAudioSource.playOnAwake = false;
            dashAudioSource.loop = false;
            dashAudioSource.spatialBlend = 0f;
        }

        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();

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

        bool grounded =
            playerController != null &&
            playerController.IsGrounded;
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
        if (playerController == null ||
        !playerController.IsGrounded)
        {
            return;
        }

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

    public void PlayDash()
    {
        if (dashAudioSource == null || dashSound == null)
        {
            return;
        }

        dashAudioSource.Stop();
        dashAudioSource.pitch = dashPitch;
        dashAudioSource.PlayOneShot(dashSound, dashVolume);
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