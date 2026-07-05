using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState { Idle, Run, DropObstacle, Laugh }
    
    [Header("AI State System")]
    public AIState currentState = AIState.Idle;
    public Transform playerTransform;
    public float detectionRange = 15f; 

    [Header("AI Navigation Settings")]
    public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent; 

    [Header("Spawning Settings")]
    public GameObject slimePrefab;
    public Transform dropPoint; 
    private float spawnTimer = 0f;
    public float spawnInterval = 3.0f; 

    [Header("Slowing Settings for Projectile Collision")]
    private float normalSpeed;
    private bool isSlowed = false;

    [Header("Troll Waddle & Animation Tuning")]
    public float waddleSpeed = 10f;      
    public float tiltIntensity = 18f;    
    [Tooltip("CRITICAL: Drag 'Ch19' or your mesh container here, NOT 'mixamorig1:Hips'!")]
    public Transform visualMeshTransform; 
    private Animator anim;
    
    private bool isJumping = false;
    private bool isDroppingObstacle = false;
    private TrollAudio trollAudio;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        
        agent.autoTraverseOffMeshLink = false;

        CommandAIToNextNode();
        normalSpeed = agent.speed;

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindWithTag("Player")?.transform;
        }

        trollAudio = GetComponent<TrollAudio>();
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                HandleIdleState();
                break;
            case AIState.Run:
                HandleRunState();
                break;
            case AIState.DropObstacle:
                HandleDropObstacleState();
                break;
            case AIState.Laugh:
                HandleLaughState();
                break;
        }
    }

    private void HandleIdleState()
    {
        if (anim) anim.SetBool("IsRunning", false);
        ResetWaddleOrientation();

        if (waypoints.Length > 0)
        {
            currentState = AIState.Run;
        }
    }

    private void HandleRunState()
    {
        if (agent.isOnNavMesh && agent.isOnOffMeshLink && !isJumping)
        {
            StartCoroutine(TriggerTrollJump());
            return;
        }

        if (isJumping || isDroppingObstacle) return;

        if (anim) anim.SetBool("IsRunning", true);

        EvaluateWaypointProgress();

        if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) < 5f)
        {
            agent.speed = isSlowed ? normalSpeed * 0.5f : normalSpeed * 1.3f; 
        }
        else if (!isSlowed)
        {
            agent.speed = normalSpeed;
        }

        ApplyProceduralWaddle();

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            currentState = AIState.DropObstacle;
        }
    }

    private void HandleDropObstacleState()
    {
        if (isJumping || isDroppingObstacle) return;
        StartCoroutine(PerformDropObstacleSequence());
    }

    private IEnumerator PerformDropObstacleSequence()
    {
        isDroppingObstacle = true;
        spawnTimer = 0f;

        // 1. Pause pathfinding so he stops to drop the trap
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;

        ResetWaddleOrientation();

        // 2. Play the attack move explicitly
        if (anim) 
        {
            anim.SetBool("IsRunning", false);
            anim.SetTrigger("Attack"); 
        }

        // 3. Drop puddle right at his feet placement coordinates
        SpawnSlimePuddle();

        // 4. Let the attack animation frame settle on screen for half a second
        yield return new WaitForSeconds(0.5f);

        // 5. BULLETPROOF OVERRIDE FIX: Force the animator to blend back into a run cycle
        // This stops him from staying frozen in the attack pose when moving again!
        if (anim)
        {
            anim.SetBool("IsRunning", true);
            anim.CrossFade("running", 0.15f); // Smoothly blends out of attack into run over 0.15s
        }

        // 6. Resume map layout navigation tracking
        if (agent != null && agent.isOnNavMesh) agent.isStopped = false;
        
        isDroppingObstacle = false;
        currentState = AIState.Run;
    }

    private void HandleLaughState()
    {
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        if (anim) 
        {
            anim.SetBool("IsRunning", false);
            anim.SetTrigger("Laugh");
        }
        ResetWaddleOrientation();
    }

    private void EvaluateWaypointProgress()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        bool reachedDestination = !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
        
        if (waypoints.Length > 0 && Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) < 2.0f)
        {
            reachedDestination = true;
        }

        if (reachedDestination)
        {
            AdvanceToNextNodeIndex();
            CommandAIToNextNode();
        }
    }

    void AdvanceToNextNodeIndex()
    {
        if (waypoints.Length == 0) return;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void CommandAIToNextNode()
    {
        if (waypoints.Length > 0 && agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    void SpawnSlimePuddle()
    {
        if (slimePrefab != null)
        {
            Vector3 spawnPosition = dropPoint != null ? dropPoint.position : (transform.position - transform.forward * 1.2f);
            spawnPosition.y = transform.position.y + 0.01f; 
            
            GameObject puddle = Instantiate(slimePrefab, spawnPosition, Quaternion.identity);
            if (puddle != null)
            {
                Debug.Log($"<color=green>[SPAWNER SUCCESS]</color> Instantiated {puddle.name} at {spawnPosition}");
                
                // Play slime drop sound
                if (trollAudio != null)
                {
                    trollAudio.PlaySlimeDrop();
                }
            }
        }
        else
        {
            Debug.LogError("[SPAWNER ERROR] Slime Prefab variable field is empty inside the Inspector layout!");
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
        float timeElapsed = 0f;

        while (timeElapsed < jumpDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / jumpDuration;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
            
            float heightArc = Mathf.Sin(t * Mathf.PI);
            currentPos.y += heightArc * 3.2f; 

            transform.position = currentPos;

            if (anim)
            {
                float slowedT = t * 0.6f; 
                anim.Play("jumping", 0, Mathf.Clamp01(slowedT)); 
            }

            yield return null;
        }

        if (agent != null && agent.isOnNavMesh && agent.isOnOffMeshLink)
        {
            agent.isStopped = false;
            agent.CompleteOffMeshLink();
        }

        if (anim)
        {
            anim.SetBool("IsRunning", true);
            anim.CrossFade("running", 0.12f); 
        }
        
        isJumping = false;
    }

    private void ApplyProceduralWaddle()
    {
        if (visualMeshTransform != null && agent.velocity.sqrMagnitude > 0.1f)
        {
            float waddleRoll = Mathf.Sin(Time.time * waddleSpeed) * tiltIntensity;
            visualMeshTransform.localRotation = Quaternion.Euler(0, 0, waddleRoll);
        }
    }

    private void ResetWaddleOrientation()
    {
        if (visualMeshTransform != null)
        {
            visualMeshTransform.localRotation = Quaternion.identity;
        }
    }

    public void TriggerVictoryState()
    {
        currentState = AIState.Laugh;
    }

    public void ApplySlow(float slowPercentage, float duration)
    {
        if (!isSlowed && agent != null)
        {
            StartCoroutine(SlowRoutine(slowPercentage, duration));
        }

        // Play troll hit sound when the troll gets hit
        if (trollAudio != null)
        {
            trollAudio.PlayHit();
        }

    }

    private IEnumerator SlowRoutine(float slowPercentage, float duration)
    {
        isSlowed = true;
        agent.speed = normalSpeed * slowPercentage;
        yield return new WaitForSeconds(duration);
        agent.speed = normalSpeed;
        isSlowed = false;
    }
}