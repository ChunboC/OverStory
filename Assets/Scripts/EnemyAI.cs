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

    [Header("Tactical Evasion Settings")]
    public float fleeRadius = 15f;
    public float safeDistance = 20f; 
    public int samplePoints = 8; 
    public float recalculatePathDistance = 2.5f;

    [Header("Game Loop Timer")]
    public float survivalTimeRequired = 45f; 
    private float currentSurvivalTime = 0f;
    private bool isMakingFinalDash = false;

    [Header("Tactical Spawning")]
    public GameObject slimePrefab;
    public Transform dropPoint; 
    public float maxDropCooldown = 8.0f; // Cooldown when player is far
    public float minDropCooldown = 2.0f; // Cooldown when player is close
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
        // Upgraded to High Quality to prevent snagging on building corners
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance; 
        
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
        if (isJumping) return; 
        if (anim) anim.SetBool("IsRunning", true);

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
        
        // Dynamically scale speed based on player proximity
        if (!isSlowed)
        {
            float speedLerp = Mathf.Clamp01((distanceToPlayer - 5f) / (safeDistance - 5f));
            agent.speed = Mathf.Lerp(panicSpeed, normalSpeed, speedLerp);
        }

        // Detect if stuck against a building (velocity near 0, but hasn't reached target)
        bool isStuck = agent.velocity.sqrMagnitude < 0.2f && agent.remainingDistance > recalculatePathDistance;

        if (!agent.pathPending && (agent.remainingDistance < recalculatePathDistance || isStuck))
        {
            Vector3 bestTarget;
            
            if (isMakingFinalDash)
            {
                Vector3 dirToBeanstalk = (beanstalkDestination.position - transform.position).normalized;
                Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
                
                if (Vector3.Dot(dirToBeanstalk, dirToPlayer) > 0.6f && distanceToPlayer < 8f) 
                {
                    bestTarget = CalculateBestEvasionPoint(); 
                }
                else
                {
                    bestTarget = beanstalkDestination.position; 
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

        NavMeshPath testPath = new NavMeshPath(); 

        for (int i = 0; i < samplePoints; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized * fleeRadius;
            Vector3 samplePos = transform.position + new Vector3(randomDir.x, 0, randomDir.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(samplePos, out hit, 4f, NavMesh.AllAreas))
            {
                agent.CalculatePath(hit.position, testPath);
                
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

        score += distToPlayer * 2.5f; 

        // Only heavily penalize points that move closer to the player IF the player is actually a threat
        if (distToPlayer < currentDistToPlayer && currentDistToPlayer < safeDistance)
        {
            score -= 1000f; 
        }

        // If player is far, wander toward the goal to keep the AI moving continuously
        if (currentDistToPlayer > safeDistance)
        {
            score -= distToGoal * 2.0f;
        }
        else
        {
            score -= distToGoal * 1.0f;
        }

        return score;
    }

    private void HandleTacticalDrops()
    {
        if (isJumping || isDroppingObstacle) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        
        // Dynamically scale slime cooldown based on player proximity
        float cooldownLerp = Mathf.Clamp01((distanceToPlayer - 5f) / (safeDistance - 5f));
        float currentCooldown = Mathf.Lerp(minDropCooldown, maxDropCooldown, cooldownLerp);

        if (Time.time >= lastDropTime + currentCooldown)
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

        // Reduced wait state to prevent movement locking while upper body anim plays
        yield return new WaitForSeconds(0.25f);
        
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
        
        if (gameManager != null)
        {
            gameManager.LoseGame(); 
        }
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