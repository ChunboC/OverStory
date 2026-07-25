using UnityEngine;

public class FallReset : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("The Y position below which the player resets.")]
    public float thresholdY = -10f; 

    [Tooltip("Time in seconds to deduct when falling off the map.")]
    public float timePenalty = 3f;

    [Header("References")]
    public GameManager gameManager;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private Rigidbody rb;
    private CharacterController characterController;

    private void Start()
    {
        // Save initial position and rotation as the respawn point
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        // Cache components if present
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (transform.position.y < thresholdY)
        {
            RespawnPlayer();
        }
    }

    private void RespawnPlayer()
    {
        // Deduct time from the timer
        if (gameManager != null)
        {
            gameManager.DeductTime(timePenalty);
        }

        // If using CharacterController, disable it temporarily so transform moves work properly
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        // Reset position and rotation
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        // Re-enable CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        // Zero out momentum if using 3D Physics (Rigidbody)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}