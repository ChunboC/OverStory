using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    
    private Rigidbody rb;
    private float movementX;
    private float movementY;
    private bool isGrounded;
    private float mouseX;
    private float mouseY;
    private int jumpsRemaining = 0;

    public float speed = 10f;
    public float turnSpeed = 15f;
    public float jumpForce = 50f;
    public int maxJumps = 2;

    public Transform cameraPivot;

    public float mouseSensitivity = 0.1f;
    public float controllerLookSensitivity = 300f;
    public float upperLookLimit = 80f;  // Max angle looking up
    public float lowerLookLimit = -40f; // Max angle looking down
    public float extraGravity = 40f;

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
    }

    void OnLook(InputValue lookValue)
    {
        
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

        // 2. Calculate movement direction relative to where the CAMERA is looking
        Vector3 moveDirection = (camForward * movementY) + (camRight * movementX);
        rb.AddForce(moveDirection.normalized * speed);

        // 3. Make the player model look in the direction they are physically moving
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetPlayerRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPlayerRotation, turnSpeed * Time.deltaTime);
        }

        // Reset jump count on landing. The velocity check prevents re-granting a jump
        // the frame the raycast still hits the platform we just jumped off.
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 2.0f);
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            jumpsRemaining = maxJumps;
        }

        if (!isGrounded)
        {
            rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
        }


        Vector2 moveValue = moveAction.ReadValue<Vector2>();

        anim.SetFloat("Pos X", moveValue.x);
        anim.SetFloat("Pos Y", moveValue.y);
    }

    void LateUpdate()
    {
        if (gameManager != null && gameManager.pauseMenuUI.activeSelf)
        {
            return;
        }

        if (lookAction != null)
        {
            Vector2 lookVector = lookAction.ReadValue<Vector2>();

            bool usingGamepad = lookAction.activeControl != null &&
                                lookAction.activeControl.device is Gamepad;

            if (usingGamepad)
            {
                mouseX += lookVector.x * controllerLookSensitivity * Time.deltaTime;
                mouseY -= lookVector.y * controllerLookSensitivity * Time.deltaTime;
            }
            else
            {
                mouseX += lookVector.x * mouseSensitivity;
                mouseY -= lookVector.y * mouseSensitivity;
            }

            mouseY = Mathf.Clamp(mouseY, lowerLookLimit, upperLookLimit);
        }

        cameraPivot.rotation = Quaternion.Euler(mouseY, mouseX, 0f);
    }

    void OnJump()
    {
        if (jumpsRemaining <= 0) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsRemaining--;

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
