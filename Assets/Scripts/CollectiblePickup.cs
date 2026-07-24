using UnityEngine;

public class collectiblePickup : MonoBehaviour
{
    [Header("Collectible Sound")]
    public AudioClip collectSound;

    [Range(0f, 1f)]
    public float collectVolume = 0.7f;

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

                // Play collectible pickup sound
                if (collectSound != null)
                {
                    AudioSource.PlayClipAtPoint(collectSound, transform.position, collectVolume);
                }

                Destroy(gameObject);
            }
        }
    }
}