using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class TrollAudio : MonoBehaviour
{
    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Troll Sounds")]
    public AudioClip[] footstepClips;
    public AudioClip slimeDropSound;
    public AudioClip hitSound;
    public AudioClip attackSound;

    [Header("Sound Volumes")]
    [Range(0f, 1f)] public float footstepVolume = 0.6f;
    [Range(0f, 1f)] public float slimeDropVolume = 0.7f;
    [Range(0f, 1f)] public float hitVolume = 0.8f;
    [Range(0f, 1f)] public float attackVolume = 0.8f;

    [Header("Footstep Settings")]
    public float stepInterval = 0.5f;
    public float minMoveSpeed = 0.1f;

    private NavMeshAgent agent;
    private float stepTimer;

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        agent = GetComponent<NavMeshAgent>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
    }

    void Update()
    {
        HandleFootsteps();
    }

    void HandleFootsteps()
    {
        if (agent == null || audioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        bool isMoving = agent.velocity.magnitude > minMoveSpeed;

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

    public void PlaySlimeDrop()
    {
        PlaySound(slimeDropSound, slimeDropVolume);
    }

    public void PlayHit()
    {
        PlaySound(hitSound, hitVolume);
    }

    public void PlayAttack()
    {
        PlaySound(attackSound, attackVolume);
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