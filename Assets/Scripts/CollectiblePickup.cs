using UnityEngine;

public class collectiblePickup : MonoBehaviour
{
    [Header("Collectible Audio")]
    [SerializeField] private AudioClip collectSound;

    [Range(0f, 1f)]
    [SerializeField] private float collectVolume = 0.8f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected)
        {
            return;
        }

        ProjectileThrower thrower = other.GetComponentInParent<ProjectileThrower>();

        if (thrower == null)
        {
            return;
        }

        collected = true;

        thrower.AddShamrock();

        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(
                collectSound,
                transform.position,
                collectVolume
            );
        }

        Destroy(gameObject);
    }
}