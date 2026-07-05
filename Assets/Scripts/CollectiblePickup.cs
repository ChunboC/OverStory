using UnityEngine;

public class collectiblePickup : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ProjectileThrower thrower = other.GetComponent<ProjectileThrower>();

            if (thrower == null)
            {
                thrower = other.GetComponentInChildren<ProjectileThrower>();
            }

            if (thrower != null)
            {
                thrower.AddShamrock();
                Destroy(gameObject);
            }
        }
    }
}