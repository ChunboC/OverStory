using UnityEngine;

public class SlowingProjectile : MonoBehaviour
{
    public float slowAmount = 0.5f; // 50% slow
    public float slowDuration = 5f;

    private bool hasHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit)
        {
            return;
        }

        Debug.Log($"PROJECTILE HIT: '{collision.gameObject.name}'");

        // Ignore collisions with the player.
        if (collision.transform.root.CompareTag("Player"))
        {
            return;
        }

        // Check whether the shamrock hit the tutorial trainer.
        ShamrockTrainerNPC trainer =
            collision.collider.GetComponentInParent<ShamrockTrainerNPC>();

        if (trainer != null)
        {
            hasHit = true;

            Debug.Log("SUCCESS: Shamrock hit the trainer!");

            trainer.ReceiveShamrockHit();

            Destroy(gameObject);
            return;
        }

        // Check whether the shamrock hit the regular enemy.
        EnemyAI enemy =
            collision.collider.GetComponentInParent<EnemyAI>();

        if (enemy != null)
        {
            hasHit = true;

            Debug.Log("SUCCESS: Hit enemy and applied slow!");

            enemy.ApplySlow(slowAmount, slowDuration);

            Destroy(gameObject);
        }
    }
}