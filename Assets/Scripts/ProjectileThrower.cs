using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileThrower : MonoBehaviour
{
    public Transform throwPoint;
    public GameObject objectToThrow;

    public float throwForce = 15f;
    public float upwardForce = 5f;

    private int shamrockCount = 0;

    public Camera playerCamera;
    private LeprechaunPlayerAudio leprechaunAudio;

    [Header("Game Manager")]
    public GameManager gameManager;

    void Start()
    {
        leprechaunAudio = GetComponent<LeprechaunPlayerAudio>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager != null && gameManager.pauseMenuUI.activeSelf)
        {
            return;
        }
        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)|| 
            (Gamepad.current != null &&
            (Gamepad.current.leftTrigger.wasPressedThisFrame ||
            Gamepad.current.rightTrigger.wasPressedThisFrame)))
        {
            if (shamrockCount > 0)
            {
                shamrockCount--;

                Vector3 camForward = playerCamera.transform.forward;
                Vector3 camUp = playerCamera.transform.up;

                GameObject projectile = Instantiate(objectToThrow, throwPoint.position, playerCamera.transform.rotation);

                Rigidbody rb = projectile.GetComponent<Rigidbody>();

                Vector3 forceToApply = (camForward * throwForce) + (camUp * upwardForce);

            rb.AddForce(forceToApply, ForceMode.Impulse);

            // Play leprechaun shoot sound
                if (leprechaunAudio != null)
                {
                    leprechaunAudio.PlayShoot();
                }
            }
        }
    }

    public void AddShamrock()
    {
        shamrockCount++;
        Debug.Log($"Collected projectile! Current amount: {shamrockCount}");
    }

}
