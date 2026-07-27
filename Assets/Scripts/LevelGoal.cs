using UnityEngine;
using UnityEngine.SceneManagement;

// Drop this on the Level 1 Beanstalk. It banks whatever shamrocks the player is
// carrying and moves them on to the next scene.
[RequireComponent(typeof(BoxCollider))]
public class LevelGoal : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Scene to load once the player reaches this goal.")]
    public string nextSceneName = "WinScene";

    [Header("References")]
    public GameManager gameManager;

    [Header("Behaviour")]
    [Tooltip("Carry the player's shamrock count into the next scene as starting ammo.")]
    public bool bankShamrocks = true;

    private bool triggered;

    private void Reset()
    {
        // Give the goal a sensible catch volume the first time it is added.
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 6f, 4f);
        box.center = new Vector3(0f, 3f, 0f);
    }

    private void Start()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        if (!box.isTrigger)
        {
            Debug.LogWarning($"[LevelGoal] Collider on '{name}' is not a trigger. Forcing isTrigger on.", this);
            box.isTrigger = true;
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (bankShamrocks)
        {
            ProjectileThrower thrower = other.GetComponent<ProjectileThrower>();

            if (thrower == null)
            {
                thrower = other.GetComponentInChildren<ProjectileThrower>();
            }

            RunProgress.BankLevel1(thrower != null ? thrower.ShamrockCount : 0);
        }

        // Stop the Level 1 timer so it cannot fire a loss during the load.
        if (gameManager != null)
        {
            gameManager.gamePlaying = false;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[LevelGoal] nextSceneName is empty - nowhere to go.", this);
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box == null) return;

        Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
