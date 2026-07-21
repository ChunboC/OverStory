using UnityEngine;
using System.Collections;

public class WeatherAI : MonoBehaviour
{
    [Header("Targeting & Prediction")]
    public Transform playerTransform;
    public Rigidbody playerRb; 
    
    [Header("Lightning Spawning")]
    public GameObject lightningWarningPrefab; 
    public GameObject lightningStrikePrefab;  
    
    // Updated your base settings here:
    private float strikeCooldown = 6.0f;
    private float warningDuration = 0.75f;
    private float blastRadius = 4.5f;

    [Header("Hazard Effects")]
    // Updated your knockback force here:
    public float knockbackForce = 100f;

    [Header("Squall (Rain & Wind) AI")]
    public ParticleSystem rainParticles; 
    public float squallCooldown = 18f;
    public float squallDuration = 4f;
    public float windForce = 25f;

    private bool isSquallActive = false;
    private Vector3 currentWindDirection;
    private float nextSquallTime;

    [Header("Escalation Timer")]
    private float nextStrikeTime;
    private float timeElapsed = 0f;
    private int currentPhase = 1;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerRb = player.GetComponent<Rigidbody>();
            }
        }
        nextStrikeTime = Time.time + strikeCooldown;
        nextSquallTime = Time.time + squallCooldown; 
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        UpdateDifficultyPhase();

        // Lightning Loop
        if (Time.time >= nextStrikeTime && playerTransform != null)
        {
            StartCoroutine(ExecuteLightningStrike());
            nextStrikeTime = Time.time + strikeCooldown;
        }

        // Squall Loop
        if (Time.time >= nextSquallTime && !isSquallActive && playerTransform != null)
        {
            StartCoroutine(ExecuteSquall());
        }
    }

    void FixedUpdate()
    {
        if (isSquallActive && playerRb != null)
        {
            playerRb.AddForce(currentWindDirection * windForce, ForceMode.Force);
        }
    }

    private IEnumerator ExecuteSquall()
    {
        isSquallActive = true;
        
        Vector3 playerVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        
        if (playerVelocity.magnitude > 2f)
        {
            currentWindDirection = (-playerVelocity.normalized + (Random.insideUnitSphere * 0.5f));
            currentWindDirection.y = 0; 
            currentWindDirection.Normalize();
        }
        else
        {
            Vector2 rand = Random.insideUnitCircle.normalized;
            currentWindDirection = new Vector3(rand.x, 0, rand.y);
        }

        Debug.Log("<color=cyan>[WEATHER AI]</color> Squall Active! Wind Direction: " + currentWindDirection);

        if (rainParticles != null) rainParticles.Play();

        float originalDrag = playerRb.linearDamping;
        playerRb.linearDamping = 0f; 

        yield return new WaitForSeconds(squallDuration);

        isSquallActive = false;
        playerRb.linearDamping = originalDrag; 
        if (rainParticles != null) rainParticles.Stop();
        
        nextSquallTime = Time.time + squallCooldown;
    }

    private void UpdateDifficultyPhase()
    {
        if (timeElapsed >= 40f && currentPhase < 3)
        {
            currentPhase = 3;
            strikeCooldown = 2.5f;
            warningDuration = 0.45f;
            blastRadius = 6.5f; // Scaled smoothly from your 4.5f base
            windForce = 40f; 
        }
        else if (timeElapsed >= 20f && timeElapsed < 40f && currentPhase < 2)
        {
            currentPhase = 2;
            strikeCooldown = 4.0f;
            warningDuration = 0.60f;
            blastRadius = 5.5f; // Scaled smoothly from your 4.5f base
            windForce = 30f; 
        }
    }

    private IEnumerator ExecuteLightningStrike()
    {
        Vector3 predictedPosition = playerTransform.position;
        if (playerRb != null)
        {
            Vector3 flatVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
            predictedPosition += flatVelocity * warningDuration;
        }
        predictedPosition.y = 0.5f; 

        GameObject warning = null;
        if (lightningWarningPrefab != null)
        {
            warning = Instantiate(lightningWarningPrefab, predictedPosition, Quaternion.identity);
            warning.transform.localScale = new Vector3(blastRadius, 0.01f, blastRadius);
        }
        
        yield return new WaitForSeconds(warningDuration);
        
        if (warning != null) Destroy(warning);
        if (lightningStrikePrefab != null)
        {
            GameObject strike = Instantiate(lightningStrikePrefab, predictedPosition, Quaternion.identity);
            Destroy(strike, 1.5f); 
        }
        
        float distanceToPlayer = Vector3.Distance(
            new Vector3(playerTransform.position.x, 0, playerTransform.position.z), 
            new Vector3(predictedPosition.x, 0, predictedPosition.z)
        );
        
        if (distanceToPlayer <= blastRadius)
        {
            ApplyLightningPenalty(predictedPosition);
        }
    }

    private void ApplyLightningPenalty(Vector3 strikeCenter)
    {
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            Vector3 pushDirection = (playerTransform.position - strikeCenter).normalized;
            pushDirection.y = 2.0f; 
            playerRb.AddForce(pushDirection.normalized * knockbackForce, ForceMode.Impulse);
        }
    }
}