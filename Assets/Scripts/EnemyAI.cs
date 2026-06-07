using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Navigation Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent; 

    [Header("Spawning Settings")]
    public GameObject slimePrefab;
    private float spawnTimer = 0f;
    private float spawnInterval = 2.0f; // Maintains the faster spawn timing!

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        CommandAIToNextNode();
    }

    void Update()
    {
        // Reverted: Strictly checks path progress to move from node to node
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            AdvanceToNextNodeIndex();
            CommandAIToNextNode();
        }

        // Handle slime puddle generation timing
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnSlimePuddle();
            spawnTimer = 0f;
        }
    }

    void AdvanceToNextNodeIndex()
    {
        if (waypoints.Length == 0) return;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void CommandAIToNextNode()
    {
        if (waypoints.Length > 0 && agent != null)
        {
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void SpawnSlimePuddle()
    {
        if (slimePrefab != null)
        {
            Instantiate(slimePrefab, transform.position, Quaternion.identity);
            Debug.Log("Troll dropped a slime puddle!");
        }
    }
}