using UnityEngine;

public class SlimeTrigger : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("<color=cyan>[SLIME OBSTACLE]</color> Player is colliding with a solid slime blocker.");
        }
    }
}