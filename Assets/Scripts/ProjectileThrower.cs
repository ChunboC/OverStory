using UnityEngine;
using UnityEngine.InputSystem;

public class ProjectileThrower : MonoBehaviour
{
    public Transform throwPoint;
    public GameObject objectToThrow;

    public float throwForce = 15f;
    public float upwardForce = 5f;

    public Camera playerCamera;

    // Update is called once per frame
    void Update()
    {
        if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.leftTrigger.wasPressedThisFrame))
        {

            Vector3 camForward = playerCamera.transform.forward;
            Vector3 camUp = playerCamera.transform.up;

            GameObject projectile = Instantiate(objectToThrow, throwPoint.position, playerCamera.transform.rotation);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();

            Vector3 forceToApply = (camForward * throwForce) + (camUp * upwardForce);

            rb.AddForce(forceToApply, ForceMode.Impulse);
        }
    }

}
