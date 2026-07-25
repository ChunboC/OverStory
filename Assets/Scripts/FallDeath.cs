using UnityEngine;

public class FallDeath : MonoBehaviour
{
    [Header("Fall Settings")]
    [Tooltip("The Y position below which the player dies.")]
    public float thresholdY = -10f; 

    public GameManager gameManager;

    private void Update()
    {
        if (transform.position.y < thresholdY)
        {
            gameManager.LoseGame();
        }
    }

}