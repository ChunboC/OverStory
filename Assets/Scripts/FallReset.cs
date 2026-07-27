using UnityEngine;
using System.Collections;

public class FallReset : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("The Y position below which the player resets.")]
    public float thresholdY = -10f; 

    [Tooltip("Time in seconds to deduct when falling off the map.")]
    public float timePenalty = 3f;

    [Header("References")]
    public GameManager gameManager;

    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private Rigidbody rb;
    private PlayerController playerController;

    private bool isRespawning; // Flag to prevent multiple triggers in a single fall

    private void Start()
    {
        // Save initial position and rotation as the respawn point
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;

        // Cache components if present
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();

        isRespawning = false;
    }

    private void Update()
    {
        if (isRespawning) return;
        
        if (Time.frameCount % 30 == 0)
        {
            UpdateSafePosition();
        }
        
        if (transform.position.y < thresholdY)
        {
            StartCoroutine(RespawnPlayer());
        }
    }

    private void UpdateSafePosition()
    {
        if (playerController != null)
        {
            if (playerController.IsGrounded && transform.position.y >= -1) // Only save position when touching safe ground
            {
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1.5f))
                {
                    if (hit.collider.CompareTag("MovingPlatform"))  // Ignore moving platforms when saving safe ground
                    {
                        return;
                    }
                }
                //Debug.Log("updating safe resparwn position");
                lastSafePosition = transform.position;
                lastSafeRotation = transform.rotation;
            }
        }
    }

    private IEnumerator RespawnPlayer()
    {
        isRespawning = true;
        
        // Deduct time from the timer
        if (gameManager != null)
        {
            Debug.Log("Dedecting time!");
            gameManager.DeductTime(timePenalty);
        }

        // If using CharacterController, disable it temporarily so transform moves work properly
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Reset position and rotation
        transform.position = lastSafePosition;
        transform.rotation = lastSafeRotation;

        // Re-enable CharacterController
        if (playerController != null)
        {
            playerController.enabled = true;
        }

        // Zero out momentum if using Rigidbody
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();

        yield return new WaitForSeconds(2);

        isRespawning = false;
    }
}