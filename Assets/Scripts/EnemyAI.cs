

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Evade, Jump, Escaped, Laugh }
    public GameManager gameManager;

    [Header("AI State & Core Targets")]
    public AIState currentState = AIState.Idle;
    public Transform playerTransform;
    public Transform beanstalkDestination;
    private NavMeshAgent agent;
    private Animator anim;
    private TrollAudio trollAudio;

    [Header("Waypoint Course Progression")]
    public GameObject startingWaypoint;
    public List<WaypointBranch> pathIntersections = new List<WaypointBranch>();
    public float waypointArrivalRadius = 2.0f;

    private GameObject currentTargetWaypoint;
    private bool isWaitingForPlayer = false;
    public float minPauseLen = 3f;
    public float maxPauseLen = 8f;

    [System.Serializable]
    public struct WaypointBranch
    {
        public GameObject currentPlatform;
        public List<GameObject> nextChoices;
    }

    [Header("Tactical Evasion Settings")]
    public float safeDistance = 15f;
    public float recalculatePathDistance = 2.5f;

    [Header("Game Loop Timer")]
    public float survivalTimeRequired = 45f;
    private float currentSurvivalTime = 0f;
    private bool isMakingFinalDash = false;

    [Header("Tactical Spawning")]
    public GameObject slimePrefab;
    public Transform dropPoint;
    public float obstacleCooldown = 4.0f;
    public float periodicDropCooldown = 8.0f;
    private float lastDropTime = 0f;

    [Header("Movement & Animation")]
    public float normalSpeed = 10f;
    public float accelerationSpeed = 10f;
    public float panicSpeed = 14f;
    public float waddleSpeed = 10f;
    public float tiltIntensity = 18f;
    public Transform visualMeshTransform;

    private bool isJumping = false;
    private bool isDroppingObstacle = false;
    private AIState stateBeforeJump = AIState.Evade;
    private float normalSpeedCache;
    private bool isSlowed = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        trollAudio = GetComponent<TrollAudio>();

        agent.autoTraverseOffMeshLink = false;
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

        normalSpeedCache = normalSpeed;
        agent.speed = normalSpeed;
        agent.acceleration = accelerationSpeed;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
        }

        if (startingWaypoint != null)
        {
            currentTargetWaypoint = startingWaypoint;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(startingWaypoint.transform.position, out navHit, 6.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
            else
            {
                agent.SetDestination(startingWaypoint.transform.position);
            }
        }
        else if (beanstalkDestination != null)
        {
            agent.SetDestination(beanstalkDestination.position);
        }

        currentState = AIState.Evade;
    }

    void Update()
    {
        if (agent.isOnNavMesh && agent.isOnOffMeshLink && currentState != AIState.Jump && !isJumping)
        {
            stateBeforeJump = currentState;
            currentState = AIState.Jump;
            StartCoroutine(TriggerTrollJump());
            return;
        }

        if (currentState == AIState.Evade)
        {
            EnforceNavMeshGroundingBounds();
        }

        switch (currentState)
        {
            case AIState.Idle:
                if (anim) anim.SetBool("IsRunning", false);
                ResetWaddleOrientation();
                break;
            case AIState.Evade:
                if (!isSlowed)
                {
                    HandleEvasion();
                    HandleTacticalDrops();
                }
                break;
            case AIState.Jump:
                if (anim) anim.SetBool("IsRunning", false);
                ResetWaddleOrientation();
                break;
            case AIState.Escaped:
            case AIState.Laugh:
                if (agent.isOnNavMesh) agent.isStopped = true;
                ResetWaddleOrientation();
                break;
        }
    }

    private void HandleEvasion()
    {
        if (isJumping || isWaitingForPlayer || currentState != AIState.Evade) return;
        if (anim && !anim.GetBool("isHit")) anim.SetBool("IsRunning", true);

        ApplyProceduralWaddle();

        if (!isMakingFinalDash)
        {
            currentSurvivalTime += Time.deltaTime;
            if (currentSurvivalTime >= survivalTimeRequired)
            {
                isMakingFinalDash = true;
                Debug.Log("<color=magenta>[AI PHASE SHIFT]</color> 45 seconds elapsed! Troll dashing to Beanstalk.");
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (!isSlowed)
        {
            agent.speed = distanceToPlayer < safeDistance/3.0 ? panicSpeed : normalSpeed;
        }

        if (isMakingFinalDash)
        {
            if (!agent.pathPending && agent.remainingDistance < recalculatePathDistance)
            {
                agent.SetDestination(beanstalkDestination.position);
            }
            return;
        }

        if (currentTargetWaypoint != null && !agent.pathPending && agent.remainingDistance <= waypointArrivalRadius)
        {
            if (playerTransform != null && distanceToPlayer > safeDistance)
            {
                StartCoroutine(WaypointWaitRoutine());
            }
            else
            {
                AdvanceToNextWaypointBranch();
            }
        }
    }

    private IEnumerator WaypointWaitRoutine()
    {
        isWaitingForPlayer = true;
        agent.isStopped = true;
        if (anim) anim.SetBool("IsRunning", false);

        currentState = AIState.Laugh;

        float pauseDuration = Random.Range(minPauseLen, maxPauseLen);
        yield return new WaitForSeconds(pauseDuration);

        agent.isStopped = false;
        isWaitingForPlayer = false;

        currentState = AIState.Evade;

        AdvanceToNextWaypointBranch();
    }

    private void AdvanceToNextWaypointBranch()
    {
        GameObject nextPlatform = null;

        foreach (var branch in pathIntersections)
        {
            if (branch.currentPlatform == currentTargetWaypoint && branch.nextChoices != null && branch.nextChoices.Count > 0)
            {
                int randomIndex = Random.Range(0, branch.nextChoices.Count);
                nextPlatform = branch.nextChoices[randomIndex];
                break;
            }
        }

        if (nextPlatform != null)
        {
            //Debug.Log($"NEXT PLATFORM: {nextPlatform}");
            currentTargetWaypoint = nextPlatform;

            NavMeshHit navHit;
            if (NavMesh.SamplePosition(nextPlatform.transform.position, out navHit, 6.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
            else
            {
                agent.SetDestination(currentTargetWaypoint.transform.position);
            }
        }
        else if (beanstalkDestination != null)
        {
            //Debug.Log($"HEADING FOR BEANSTALK");
            currentTargetWaypoint = null;
            agent.SetDestination(beanstalkDestination.position);
        }
    }

    private float EvaluatePosition(Vector3 candidatePos)
    {
        float score = 0;
        float distToPlayer = Vector3.Distance(candidatePos, playerTransform.position);
        float distToGoal = Vector3.Distance(candidatePos, beanstalkDestination.position);
        float currentDistToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer < currentDistToPlayer)
        {
            score -= 1000f;
        }

        score += distToPlayer * 2.5f;
        score -= distToGoal * 1.0f;

        return score;
    }

    private void HandleTacticalDrops()
    {
        if (Time.time < lastDropTime + obstacleCooldown || isJumping || isDroppingObstacle) return;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, dirToPlayer);


        bool playerIsCloseBehind = (dotProduct < -0.6f && Vector3.Distance(transform.position, playerTransform.position) < safeDistance);
        bool periodicDropReady = (Time.time >= lastDropTime + periodicDropCooldown);
        if (playerIsCloseBehind || periodicDropReady)
        {
            StartCoroutine(PerformDropObstacleSequence());
        }
    }
    private IEnumerator PerformDropObstacleSequence()
    {
        isDroppingObstacle = true;
        lastDropTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");
        SpawnSlimePuddle();
        yield return new WaitForSeconds(1.2f);
        isDroppingObstacle = false;
    }
    void SpawnSlimePuddle()
    {
        if (slimePrefab != null)
        {
            Vector3 basePos = dropPoint != null ? dropPoint.position : (transform.position - transform.forward * 2.5f);
            Vector3 spawnPosition = basePos;
            Collider[] hits = Physics.OverlapSphere(basePos, 1.5f, LayerMask.GetMask("Building"));
            if (hits.Length > 0)
            {
                spawnPosition = basePos + (transform.forward * 2.5f);
            }
            Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
        }
    }
    private void ApplyProceduralWaddle()
    {
        if (visualMeshTransform == null) return;
        float waddle = Mathf.Sin(Time.time * waddleSpeed) * tiltIntensity;
        visualMeshTransform.localRotation = Quaternion.Euler(0, 0, waddle);
    }
    private void ResetWaddleOrientation()
    {
        if (visualMeshTransform == null) return;
        visualMeshTransform.localRotation = Quaternion.identity;
    }
    private void EnforceNavMeshGroundingBounds()
    {
        if (agent.isOnOffMeshLink || isJumping || currentState == AIState.Jump) return;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        {
            if (transform.position.y < hit.position.y - 0.1f)
            {
                transform.position = new Vector3(transform.position.x, hit.position.y, transform.position.z);
            }
        }
    }
    private IEnumerator TriggerTrollJump()
    {
        isJumping = true;

        if (anim) anim.SetTrigger("JumpTrigger");

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = linkData.endPos;

        float jumpDuration = 0.8f;
        float jumpHeight = 4.0f;
        float normalizedTime = 0.0f;

        while (normalizedTime < 1.0f)
        {
            normalizedTime += Time.deltaTime / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, normalizedTime);
            currentPos.y += Mathf.Sin(normalizedTime * Mathf.PI) * jumpHeight;

            transform.position = currentPos;

            yield return null;
        }

        transform.position = endPos;
        agent.velocity = Vector3.zero;
        agent.CompleteOffMeshLink();
        agent.Warp(endPos);

        isJumping = false;
        currentState = stateBeforeJump;
    }


    public void ApplySlow(float slowPercentage, float duration)
    {
        if (!isSlowed && agent != null)
        {
            StartCoroutine(SlowRoutine(slowPercentage, duration));
        }
        if (trollAudio != null) trollAudio.PlayHit();
    }

    private IEnumerator SlowRoutine(float slowPercentage, float duration)
    {
        isSlowed = true;
        anim.SetBool("isHit", true);
        agent.isStopped = true;
        agent.speed = 0;
        yield return new WaitForSeconds(duration);
        agent.speed = normalSpeedCache;
        isSlowed = false;
        agent.isStopped = false;
        anim.SetBool("isHit", false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Player collided with something!");

        if (collision.gameObject.CompareTag("Beanstalk"))
        {
            Debug.Log("Enemy reached the beanstalk!");

            if (gameManager != null)
            {
                gameManager.LoseGame();
            }
        }
    }
}