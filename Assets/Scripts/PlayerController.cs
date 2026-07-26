using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

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

    // For cannon 
    public bool IsGrounded => isGrounded;
    public bool ControlsLocked { get; private set; }


    private static readonly int JumpState =
        Animator.StringToHash("Base Layer.jump");

    private static readonly int DashState =
        Animator.StringToHash("Base Layer.Dash");

    public float groundCheckDelayAfterJump = 0.1f;

    public float speed = 5f;
    public float turnSpeed = 15f;
    public float jumpForce = 50f;
    public int maxJumps = 2;

    [Header("Ability Unlocks")]
    [SerializeField] private bool doubleJumpUnlocked = false;
    [SerializeField] private bool airDashUnlocked = false;

    public bool DoubleJumpUnlocked => doubleJumpUnlocked;
    public bool AirDashUnlocked => airDashUnlocked;

    [Header("Movement Control")]
    public float groundAcceleration = 35f;
    public float airAcceleration = 8f;
    public float fallGravityMultiplier = 2.5f;

    [Header("Air Dash")]
    public float airDashSpeed = 16f;
    public float airDashDuration = 0.18f;

    private bool airDashAvailable = true;
    private bool isAirDashing = false;
    

    [Header("Camera Control")]
    public Transform cameraPivot;
    public float mouseSensitivity = 0.1f;
    public float upperLookLimit = 80f;  // Max angle looking up
    public float lowerLookLimit = -40f; // Max angle looking down
    public float controllerLookSpeed = 160f;
    public float rightStickDeadZone = 0.15f;

    [Header("Game Manager")]
    public GameManager gameManager;

    private Animator anim;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private LeprechaunPlayerAudio leprechaunAudio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; 
        anim = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        leprechaunAudio = GetComponent<LeprechaunPlayerAudio>();
        mouseX = transform.eulerAngles.y;
        mouseY = 0f;
        jumpsRemaining = maxJumps;
    }

    void LateUpdate()
    {
        Vector2 lookValue = lookAction.ReadValue<Vector2>();

        bool usingGamepad =
            lookAction.activeControl?.device is Gamepad;

        if (usingGamepad)
        {
            // Right stick controls the camera.
            if (lookValue.sqrMagnitude >
                rightStickDeadZone * rightStickDeadZone)
            {
                mouseX +=
                    lookValue.x *
                    controllerLookSpeed *
                    Time.deltaTime;

                mouseY -=
                    lookValue.y *
                    controllerLookSpeed *
                    Time.deltaTime;
            }
        }
        else
        {
            // Mouse controls the camera when using keyboard and mouse.
            mouseX += lookValue.x * mouseSensitivity;
            mouseY -= lookValue.y * mouseSensitivity;
        }

        mouseY = Mathf.Clamp(
            mouseY,
            lowerLookLimit,
            upperLookLimit
        );

        cameraPivot.rotation = Quaternion.Euler(
            mouseY,
            mouseX,
            0f
        );
    }

    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
        Debug.Log("Double jump unlocked.");
    }

    public void UnlockAirDash()
    {
        airDashUnlocked = true;
        Debug.Log("Air dash unlocked.");
    }

    void OnMove (InputValue movementValue)
    {
        // cannon check
        if (ControlsLocked)
        {
            return;
        }

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

        bool animatorGrounded =
            isGrounded &&
            Time.time - lastJumpTime > 0.1f;

        anim.SetBool("IsGrounded", animatorGrounded);

        if (isGrounded && !isAirDashing)
        {
            airDashAvailable = true;
        }

        if (isAirDashing)
        {
            return;
        }

        // cannon check
        if (ControlsLocked)
        {
            return;
        }


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

        // How far the left stick is tilted.
        // 0 = no movement, 1 = fully tilted.
        float inputMagnitude = Mathf.Clamp01(moveDirection.magnitude);

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
                // Slight stick tilt gives walking speed.
                // Full stick tilt gives running speed.
                Vector3 desiredRelativeVelocity = new Vector3(
                    appliedDirection.x * speed * inputMagnitude,
                    0f,
                    appliedDirection.z * speed * inputMagnitude
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
                moveDirection.normalized * speed * inputMagnitude;

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

    void OnJump()
    {
        // cannon check
        if (ControlsLocked)
        {
            return;
        }

        if (jumpsRemaining <= 0) return;

        lastJumpTime = Time.time;
        isGrounded = false;
        anim.SetBool("IsGrounded", false);

        // The first jump starts with all jumps available.
        // Any jump after that requires the double-jump ability.
        bool attemptingSecondJump = jumpsRemaining < maxJumps;

        if (attemptingSecondJump && !doubleJumpUnlocked)
        {
            return;
        }

        lastJumpTime = Time.time;
        isGrounded = false;
        anim.SetBool("IsGrounded", false);

        // Directly start the Jump state.
        // Calling this again restarts it for the second jump.
        anim.Play(JumpState, 0, 0f);

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsRemaining--;

        // Prevent the ground check from treating takeoff as a landing.
        

        if (leprechaunAudio != null)
        {
            leprechaunAudio.PlayJump();
        }
    }

    void OnDash(InputValue value)
    {
        // cannon check
        if (ControlsLocked)
        {
            return;
        }

        // Dash is unavailable before the trainer unlocks it.
        if (!airDashUnlocked)
        {
            return;
        }

        if (!value.isPressed)
            return;

        // Dash is only allowed while airborne.
        if (isGrounded)
            return;

        // Only one dash is allowed before landing.
        if (!airDashAvailable || isAirDashing)
            return;

        Vector3 dashDirection =
            (cameraPivot.forward * movementY) +
            (cameraPivot.right * movementX);

        // Keep the dash horizontal.
        dashDirection.y = 0f;

        // Dash forward when there is no movement input.
        if (dashDirection.sqrMagnitude < 0.01f)
        {
            dashDirection = transform.forward;
            dashDirection.y = 0f;
        }

        dashDirection.Normalize();
      
        if (leprechaunAudio != null)
        {
            leprechaunAudio.PlayDash();
        }

        StartCoroutine(AirDashRoutine(dashDirection));

    }

    private IEnumerator AirDashRoutine(Vector3 dashDirection)
    {
        isAirDashing = true;
        airDashAvailable = false;

        anim.CrossFade(DashState, 0.05f, 0, 0f);

        bool originalUseGravity = rb.useGravity;

        // Prevent the player from falling during the short dash.
        rb.useGravity = false;

        float elapsedTime = 0f;

        while (elapsedTime < airDashDuration && !isGrounded)
        {
            rb.linearVelocity =
                dashDirection * airDashSpeed;

            elapsedTime += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        rb.useGravity = originalUseGravity;
        isAirDashing = false;

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

    // when player enters cannon, movement should be locked
    public void SetControlsLocked(bool locked)
    {
        ControlsLocked = locked;

        if (locked)
        {
            movementX = 0f;
            movementY = 0f;
        }
    }

}
