using UnityEngine;

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

    [Header("Player Sound Volumes")]
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    [Range(0f, 1f)] public float shootVolume = 0.7f;

    [Header("Footstep Settings")]
    public float stepInterval = 0.45f;
    public float minMoveSpeed = 0.1f;
    public float footstepVolume = 0.6f;

    private CharacterController characterController;
    private Rigidbody rb;

    private Vector3 lastPosition;
    private float stepTimer;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        lastPosition = transform.position;

        audioSource.playOnAwake = false;
        audioSource.loop = false;

        // For player sounds, 0 makes it easy to hear.
        // Change to 1 if you want true 3D position-based footsteps.
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

        float moveSpeed = GetPlayerMoveSpeed();
        bool isMoving = moveSpeed > minMoveSpeed;

        if (isMoving)
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
            stepTimer = 0f;
        }

        lastPosition = transform.position;
    }

    float GetPlayerMoveSpeed()
    {
        if (characterController != null)
        {
            Vector3 flatVelocity = characterController.velocity;
            flatVelocity.y = 0f;
            return flatVelocity.magnitude;
        }

        if (rb != null)
        {
            Vector3 flatVelocity = rb.linearVelocity;
            flatVelocity.y = 0f;
            return flatVelocity.magnitude;
        }

        Vector3 movement = transform.position - lastPosition;
        movement.y = 0f;

        return movement.magnitude / Time.deltaTime;
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