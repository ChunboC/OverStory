using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CloudCourseAI : MonoBehaviour
{
    [Header("Course Milestones (Assign Cloud Platform GameObjects Here)")]
    [Tooltip("The very first platform or waypoint the AI should march toward.")]
    [SerializeField] private GameObject startingWaypoint;

    [Tooltip("A list of intersections. When the AI leaves a platform, it looks up that platform here to choose a random next step.")]
    [SerializeField] private List<WaypointBranch> pathIntersections = new List<WaypointBranch>();

    [Tooltip("The ultimate final finish line GameObject at the end of the entire course.")]
    [SerializeField] private Transform ultimateEndPoint;

    [Header("Player Tracking Settings")]
    [Tooltip("Drag your Player GameObject here.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("If the player is further than this many meters behind, the AI pauses.")]
    [SerializeField] private float waitDistanceThreshold = 12f;
    [SerializeField] private float minimumPauseTime = 1.5f;
    [SerializeField] private float maximumPauseTime = 3.5f;

    [Header("Navigation Rules")]
    [SerializeField] private float completionRadius = 1.5f;

    private NavMeshAgent agent;
    private GameObject currentTargetWaypoint;
    private bool isWaitingForPlayer = false;

    // A clean data wrapper to define forks in the road directly inside this inspector
    [System.Serializable]
    public struct WaypointBranch
    {
        [Tooltip("The platform the agent is currently on.")]
        public GameObject currentPlatform;
        [Tooltip("The potential next platforms the agent can choose to jump to from here.")]
        public List<GameObject> nextChoices;
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Let Unity's underlying physics engine handle your active jump link ribbons
        agent.autoTraverseOffMeshLink = true;

        if (startingWaypoint != null)
        {
            UpdateTargetDestination(startingWaypoint);
        }
        else if (ultimateEndPoint != null)
        {
            agent.SetDestination(ultimateEndPoint.position);
        }
    }

    void Update()
    {
        if (isWaitingForPlayer || currentTargetWaypoint == null) return;

        // Step 1: Detect if the agent has arrived at its immediate target platform
        if (!agent.pathPending && agent.remainingDistance <= completionRadius)
        {
            EvaluateCourseProgression();
        }
    }

    private void EvaluateCourseProgression()
    {
        // Step 2: Check if player distance tracking forces a pause
        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) > waitDistanceThreshold)
        {
            StartCoroutine(PauseRoutine());
        }
        else
        {
            CalculateNextMilestone();
        }
    }

    private IEnumerator PauseRoutine()
    {
        isWaitingForPlayer = true;
        agent.isStopped = true; // Smoothly halt pathfinding execution motors

        float pauseDuration = Random.Range(minimumPauseTime, maximumPauseTime);
        yield return new WaitForSeconds(pauseDuration);

        agent.isStopped = false;
        isWaitingForPlayer = false;

        CalculateNextMilestone();
    }

    private void CalculateNextMilestone()
    {
        GameObject nextPlatform = null;

        // Step 3: Scan the intersection lookup list to see if this platform has branches
        foreach (var branch in pathIntersections)
        {
            if (branch.currentPlatform == currentTargetWaypoint && branch.nextChoices != null && branch.nextChoices.Count > 0)
            {
                // Choose a random path index at intersection platforms
                int randomIndex = Random.Range(0, branch.nextChoices.Count);
                nextPlatform = branch.nextChoices[randomIndex];
                break;
            }
        }

        // Step 4: Advance or target final course finish line
        if (nextPlatform != null)
        {
            UpdateTargetDestination(nextPlatform);
        }
        else if (ultimateEndPoint != null)
        {
            currentTargetWaypoint = null; // Clear waypoint lock
            agent.SetDestination(ultimateEndPoint.position);
            Debug.Log($"{gameObject.name} is making its final run to the finish line!");
        }
    }

    private void UpdateTargetDestination(GameObject targetObj)
    {
        currentTargetWaypoint = targetObj;

        // Instantly maps destination to the live location vector
        agent.SetDestination(targetObj.transform.position);
    }
}
