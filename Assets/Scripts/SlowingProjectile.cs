using UnityEngine;

public class SlowingProjectile : MonoBehaviour
{
    public float slowAmount = 0.5f; // 50% slow
    public float slowDuration = 3f;  

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Log absolutely everything the flying projectile touches!
        Debug.Log($"PROJECTILE HIT: '{collision.gameObject.name}'");

        // 2. Check if we hit the enemy
        EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();

        if (enemy != null)
        {
            Debug.Log("SUCCESS: Hit enemy and applied slow!");
            enemy.ApplySlow(slowAmount, slowDuration);
            Destroy(gameObject); // Destroy instantly on enemy hit
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            // Ignore the player's face/arms if it clips them on spawn
            return;
        }
    }
}