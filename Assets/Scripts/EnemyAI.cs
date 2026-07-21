using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Evade, DropObstacle, Jump, Escaped, Laugh }
    
    [Header("AI State & Core Targets")]
    public AIState currentState = AIState.Idle;
    public Transform playerTransform;
    public Transform beanstalkDestination; 
    private NavMeshAgent agent; 
    private Animator anim;
    private TrollAudio trollAudio;

    [Header("Tactical Evasion Settings")]
    public float fleeRadius = 15f;
    public float safeDistance = 20f; 
    public int samplePoints = 8; 
    public float recalculatePathDistance = 2.5f;

    [Header("Tactical Spawning")]
    public GameObject slimePrefab;
    public Transform dropPoint; 
    public float obstacleCooldown = 4.0f;
    private float lastDropTime = 0f;

    [Header("Movement & Animation")]
    public float normalSpeed = 10f;
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
        
        agent.autoTraverseOffMeshLink = true; 
        
        agent.autoBraking = false; 

        agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance; 
        
        normalSpeedCache = normalSpeed;
        agent.speed = normalSpeed;
        agent.acceleration = 24f;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
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
                HandleEvasion();
                HandleTacticalDrops();
                break;
            case AIState.DropObstacle:
                // Handled via Coroutine
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
    if (isJumping || isDroppingObstacle) return;
    if (anim) anim.SetBool("IsRunning", true);

    ApplyProceduralWaddle();

    float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
    
    if (!isSlowed)
    {
        agent.speed = distanceToPlayer < 12f ? panicSpeed : normalSpeed;
    }

    if (!agent.pathPending && agent.remainingDistance < recalculatePathDistance)
    {
        Vector3 bestTarget;
        
        if (distanceToPlayer > safeDistance)
        {
            // Calculate directions
            Vector3 dirToBeanstalk = (beanstalkDestination.position - transform.position).normalized;
            Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
            
            // Check if player is directly in the path to the beanstalk (Dot > 0.5 means they are in front)
            if (Vector3.Dot(dirToBeanstalk, dirToPlayer) < 0.5f) 
            {
                bestTarget = beanstalkDestination.position; // Path is clear
            }
            else
            {
                bestTarget = CalculateBestEvasionPoint(); // Player is blocking, evade instead
            }
        }
        else
        {
            bestTarget = CalculateBestEvasionPoint();
        }

        agent.SetDestination(bestTarget);
    }
}

    private Vector3 CalculateBestEvasionPoint()
{
    Vector3 bestPoint = transform.position;
    float highestScore = -Mathf.Infinity;

    // Create an empty path to test our routes
    NavMeshPath testPath = new NavMeshPath(); 

    for (int i = 0; i < samplePoints; i++)
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized * fleeRadius;
        Vector3 samplePos = transform.position + new Vector3(randomDir.x, 0, randomDir.y);

        NavMeshHit hit;
        // Sample the NavMesh to find the closest valid surface
        if (NavMesh.SamplePosition(samplePos, out hit, 4f, NavMesh.AllAreas))
        {
            // NEW: Calculate a path to the sampled point
            agent.CalculatePath(hit.position, testPath);
            
            // NEW: Only evaluate the point if the path is complete (no walls blocking it)
            if (testPath.status == NavMeshPathStatus.PathComplete)
            {
                float score = EvaluatePosition(hit.position);
                if (score > highestScore)
                {
                    highestScore = score;
                    bestPoint = hit.position;
                }
            }
        }
    }
    return bestPoint;
}

    private float EvaluatePosition(Vector3 candidatePos)
{
    float score = 0;
    float distToPlayer = Vector3.Distance(candidatePos, playerTransform.position);
    float distToGoal = Vector3.Distance(candidatePos, beanstalkDestination.position);
    float currentDistToPlayer = Vector3.Distance(transform.position, playerTransform.position);

    // INSTANT REJECTION: If this point moves us closer to the player, penalize it heavily
    if (distToPlayer < currentDistToPlayer)
    {
        score -= 1000f; 
    }

    // Core AI Heuristic
    score += distToPlayer * 2.5f; 
    score -= distToGoal * 1.0f;   

    return score;
}

    private void HandleTacticalDrops()
    {
        if (Time.time < lastDropTime + obstacleCooldown || isJumping || isDroppingObstacle) return;

        // Tactical Trap Logic: Drop slime if player is directly behind us and close
        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float dotProduct = Vector3.Dot(transform.forward, dirToPlayer);

        // -1 is perfectly behind, 1 is perfectly in front
        if (dotProduct < -0.6f && Vector3.Distance(transform.position, playerTransform.position) < 14f)
        {
            currentState = AIState.DropObstacle;
            StartCoroutine(PerformDropObstacleSequence());
        }
    }

    private IEnumerator PerformDropObstacleSequence()
    {
        isDroppingObstacle = true;
        lastDropTime = Time.time;
        agent.isStopped = true;

        if (anim != null) anim.SetTrigger("Attack"); 
        
        SpawnSlimePuddle();

        yield return new WaitForSeconds(1.2f);
        
        agent.isStopped = false;
        isDroppingObstacle = false;
        currentState = AIState.Evade;
        
        // Force immediate recalculation to run away
        agent.SetDestination(CalculateBestEvasionPoint()); 
    }

    void SpawnSlimePuddle()
    {
        if (slimePrefab != null)
        {
            Vector3 basePos = dropPoint != null ? dropPoint.position : (transform.position - transform.forward * 2.5f);
            Vector3 spawnPosition = basePos;
            
            // Check for walls to prevent AI from getting stuck
            Collider[] hits = Physics.OverlapSphere(basePos, 1.5f, LayerMask.GetMask("Building"));
            if (hits.Length > 0)
            {
                spawnPosition = basePos + (transform.right * 2.0f);
            }

            spawnPosition.y = transform.position.y - 0.1f; 
            
            GameObject puddle = Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
            puddle.layer = LayerMask.NameToLayer("Slime");

            if (trollAudio != null) trollAudio.PlaySlimeDrop();
            if (puddle.GetComponent<SlimeTrigger>() == null) puddle.AddComponent<SlimeTrigger>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == beanstalkDestination)
        {
            currentState = AIState.Escaped;
            TriggerVictoryState();
        }
    }

    private IEnumerator TriggerTrollJump()
    {
        isJumping = true;
        agent.isStopped = true;
        if (anim) anim.SetBool("IsRunning", false);
        ResetWaddleOrientation();

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos;

        float jumpDuration = 1.6f; 
        for (float t = 0; t < jumpDuration; t += Time.deltaTime)
        {
            float normalizedT = t / jumpDuration;
            transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, normalizedT)) + Vector3.up * (Mathf.Sin(normalizedT * Mathf.PI) * 3.2f);
            if (anim) anim.Play("jumping", 0, Mathf.Clamp01(normalizedT * 0.7f));
            yield return null;
        }
        
        transform.position = endPos;
        if (agent.isOnNavMesh)
        {
            agent.CompleteOffMeshLink();
            agent.isStopped = false;
        }
        
        isJumping = false;
        currentState = stateBeforeJump;
    }

    private void EnforceNavMeshGroundingBounds()
    {
        if (agent.velocity.sqrMagnitude < 0.1f && !agent.pathPending && !isJumping && !isDroppingObstacle)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1.5f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) > 0.2f)
                {
                    transform.position = hit.position;
                    agent.SetDestination(CalculateBestEvasionPoint());
                }
            }
        }
    }

    private void ApplyProceduralWaddle()
    {
        if (visualMeshTransform != null && agent.velocity.sqrMagnitude > 0.1f)
            visualMeshTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * waddleSpeed) * tiltIntensity);
    }

    private void ResetWaddleOrientation() { if (visualMeshTransform != null) visualMeshTransform.localRotation = Quaternion.identity; }
    
    public void TriggerVictoryState()
    {
        currentState = AIState.Laugh;
        if (anim) { anim.SetBool("IsRunning", false); anim.SetTrigger("Laugh"); }
        Debug.Log("<color=red>[GAME OVER]</color> Troll reached the destination! Player loses the game.");
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
        agent.speed = normalSpeedCache * slowPercentage;
        yield return new WaitForSeconds(duration);
        agent.speed = normalSpeedCache;
        isSlowed = false;
    }
}