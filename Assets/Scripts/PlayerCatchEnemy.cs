using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerCatchEnemy : MonoBehaviour
{
    [Header("Catch Settings")]
    public string enemyTag = "Enemy";

    [Header("Game Manager")]
    public GameManager gameManager;

    // set game status to WinGame() when player and enemy collides
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(enemyTag))
        {
            Debug.Log("Player caught the enemy!");

            if (gameManager != null)
            {
                gameManager.WinGame();
            }
        }
    }
}
