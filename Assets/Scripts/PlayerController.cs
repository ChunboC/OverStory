using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    
    private Rigidbody rb;
    private float movementX;
    private float movementY;
    private bool isGrounded;
    private float mouseX;
    private float mouseY;
    private int jumpsRemaining = 0;
    private bool wasGrounded;
    private float lastJumpTime;

    public float groundCheckDelayAfterJump = 0.1f;

    public float speed = 5f;
    public float turnSpeed = 15f;
    public float jumpForce = 50f;
    public int maxJumps = 2;

    [Header("Movement Control")]
    public float groundAcceleration = 35f;
    public float airAcceleration = 8f;
    public float fallGravityMultiplier = 2.5f;

    public Transform cameraPivot;

    public float mouseSensitivity = 0.1f;
    public float upperLookLimit = 80f;  // Max angle looking up
    public float lowerLookLimit = -40f; // Max angle looking down

    [Header("Game Manager")]
    public GameManager gameManager;

    private Animator anim;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private LeprechaunPlayerAudio leprechaunAudio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
        anim = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        leprechaunAudio = GetComponent<LeprechaunPlayerAudio>();
        mouseX = transform.eulerAngles.y;
        mouseY = 0f;
        jumpsRemaining = maxJumps;
    }

    void OnLook(InputValue lookValue)
    {
        Vector2 lookVector = lookValue.Get<Vector2>();
        
        mouseX += lookVector.x * mouseSensitivity;
        mouseY -= lookVector.y * mouseSensitivity;

        mouseY = Mathf.Clamp(mouseY, lowerLookLimit, upperLookLimit); // Clamp up/down looking so the camera doesn't flip upside down
    }



    void OnMove (InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x; 
        movementY = movementVector.y; 
        
    }

    void FixedUpdate()
    {
        if (gameManager != null && gameManager.pauseMenuUI.activeSelf)
        {
            return;
        }

        Vector3 camForward = cameraPivot.forward;
        Vector3 camRight = cameraPivot.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        // Ground check.
        RaycastHit groundHit;
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            out groundHit,
            0.25f
        );

        // Get moving-platform velocity.
        Vector3 platformVelocity = Vector3.zero;

        if (isGrounded)
        {
            KinematicPlatform platform =
                groundHit.collider.GetComponentInParent<KinematicPlatform>();

            if (platform != null)
            {
                platformVelocity = platform.CurrentVelocity;
            }
        }

        Vector3 moveDirection =
            (camForward * movementY) +
            (camRight * movementX);

        Vector3 appliedDirection = moveDirection.normalized;

        if (isGrounded)
        {
            appliedDirection = Vector3.ProjectOnPlane(
                moveDirection.normalized,
                groundHit.normal
            ).normalized;
        }

        Vector3 currentVelocity = rb.linearVelocity;

        Vector3 currentHorizontalVelocity = new Vector3(
            currentVelocity.x,
            0f,
            currentVelocity.z
        );

        if (isGrounded)
        {
            Vector3 platformHorizontalVelocity = new Vector3(
                platformVelocity.x,
                0f,
                platformVelocity.z
            );

            Vector3 currentRelativeVelocity =
                currentHorizontalVelocity - platformHorizontalVelocity;

            Vector3 nextRelativeVelocity;

            if (moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 desiredRelativeVelocity = new Vector3(
                    appliedDirection.x * speed,
                    0f,
                    appliedDirection.z * speed
                );

                nextRelativeVelocity = Vector3.MoveTowards(
                    currentRelativeVelocity,
                    desiredRelativeVelocity,
                    groundAcceleration * Time.fixedDeltaTime
                );
            }
            else
            {
                nextRelativeVelocity = Vector3.zero;
            }

            Vector3 finalHorizontalVelocity =
                nextRelativeVelocity + platformHorizontalVelocity;

            rb.linearVelocity = new Vector3(
                finalHorizontalVelocity.x,
                currentVelocity.y,
                finalHorizontalVelocity.z
            );
        }
        else if (moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 desiredAirVelocity =
                moveDirection.normalized * speed;

            Vector3 nextAirVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                desiredAirVelocity,
                airAcceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector3(
                nextAirVelocity.x,
                currentVelocity.y,
                nextAirVelocity.z
            );
        }

        // Apply extra gravity while falling.
        if (!isGrounded && rb.linearVelocity.y < 0f)
        {
            rb.AddForce(
                Physics.gravity * (fallGravityMultiplier - 1f),
                ForceMode.Acceleration
            );
        }

        // Keep rotation code after the movement code.
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetPlayerRotation =
                Quaternion.LookRotation(moveDirection, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetPlayerRotation,
                turnSpeed * Time.deltaTime
            );
        }

        // Reset jumps only when the player actually lands.
        bool justLanded =
            isGrounded &&
            !wasGrounded &&
            Time.time - lastJumpTime > groundCheckDelayAfterJump;

        if (justLanded)
        {
            jumpsRemaining = maxJumps;
        }

        wasGrounded = isGrounded;

        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        anim.SetFloat("Pos X", moveValue.x);
        anim.SetFloat("Pos Y", moveValue.y);
    }

    void LateUpdate()
    {    
        cameraPivot.rotation = Quaternion.Euler(mouseY, mouseX, 0f); // Rotate the pivot based on mouse inputs. This keeps camera looking independent of player body!
    }

    void OnJump()
    {
        if (jumpsRemaining <= 0) return;

        lastJumpTime = Time.time;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsRemaining--;

        // Prevent the ground check from treating takeoff as a landing.
        isGrounded = false;

        if (leprechaunAudio != null)
        {
            leprechaunAudio.PlayJump();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Player collided with something!");

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Player caught the enemy!");

            if (gameManager != null)
            {
                gameManager.WinGame();
            }
        }
    }

    private void OnPause(InputValue value)
    {
        if (value.isPressed && gameManager != null)
        {
            gameManager.TogglePause();
        }
    }


}
