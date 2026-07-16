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

        // Ground check up front so we can grab the surface normal (needed to climb ramps).
        RaycastHit groundHit;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, out groundHit, 0.25f);

        // 2. Calculate movement direction relative to where the CAMERA is looking
        Vector3 moveDirection = (camForward * movementY) + (camRight * movementX);

        // Push along the slope instead of horizontally into it, otherwise the flat
        // (y = 0) direction just drives the player into a ramp face and gravity wins.
        Vector3 appliedDirection = moveDirection.normalized;
        if (isGrounded)
        {
            appliedDirection = Vector3.ProjectOnPlane(moveDirection.normalized, groundHit.normal).normalized;
        }
        rb.AddForce(appliedDirection * speed);

        // 3. Make the player model look in the direction they are physically moving
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetPlayerRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPlayerRotation, turnSpeed * Time.deltaTime);
        }

        // Reset jump count on landing. The velocity check prevents re-granting a jump
        // the frame the raycast still hits the platform we just jumped off.
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
            jumpsRemaining = maxJumps;

        Vector2 moveValue = moveAction.ReadValue<Vector2>();
        Debug.Log(moveValue);

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
