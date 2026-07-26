using UnityEngine;
using System.Collections;

public class WeatherAI : MonoBehaviour
{
    [Header("System Dependencies")]
    public GameManager gameManager;

    [Header("Targeting & Prediction")]
    public Transform playerTransform;
    public Rigidbody playerRb; 
    
    [Header("Storm Settings")]
    public float timeBetweenStorms = 15f; 
    public float stormDuration = 8f;    
    public float stormWarningTime = 2f; 
    
    public GameObject rainPrefab; 
    public float windForce = 6f; 
    
    [Header("Lightning Settings")]
    public GameObject lightningWarningPrefab; 
    public GameObject lightningStrikePrefab;  
    public float minStrikeCooldown = 1.2f; 
    public float maxStrikeCooldown = 2.2f;
    public float warningDuration = 1.5f; 
    public float blastRadius = 0.8f; 
    public float knockbackForce = 15f;

    // Active State Variables
    private bool isStormActive = false;
    private bool hasWarnedForStorm = false;
    private float nextStormTime;
    private Vector3 currentWindDirection;
    private float nextStrikeTime;
    
    // Track the currently spawned rain instance
    private GameObject activeRainInstance;

    void Start()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerRb = player.GetComponent<Rigidbody>();
            }
        }
        
        nextStormTime = Time.time + timeBetweenStorms;
    }

    void Update()
    {
        if (gameManager != null && GameManager.IsIntroPlaying)
        {
            nextStormTime = Time.time + timeBetweenStorms;
            return; 
        }

        if (isStormActive && Time.time >= nextStrikeTime && playerTransform != null)
        {
            StartCoroutine(ExecuteLightningStrike());
            nextStrikeTime = Time.time + Random.Range(minStrikeCooldown, maxStrikeCooldown);
        }

        if (!isStormActive && !hasWarnedForStorm && Time.time >= (nextStormTime - stormWarningTime))
        {
            hasWarnedForStorm = true;
        }

        if (Time.time >= nextStormTime && !isStormActive && playerTransform != null)
        {
            StartCoroutine(ExecuteStormEvent());
        }
    }

    void FixedUpdate()
    {
        if (isStormActive && playerRb != null)
        {
            playerRb.AddForce(currentWindDirection * windForce, ForceMode.Acceleration);
        }
    }

    private IEnumerator ExecuteStormEvent()
    {
        isStormActive = true;
        nextStrikeTime = Time.time + 0.5f;

        // Safely calculate a wind direction so it never accidentally equals absolute zero
        Vector3 playerVelocity = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        if (playerVelocity.magnitude > 2f)
        {
            currentWindDirection = (-playerVelocity.normalized + (Random.insideUnitSphere * 0.5f));
            currentWindDirection.y = 0; 
            if (currentWindDirection == Vector3.zero) currentWindDirection = Vector3.forward;
            currentWindDirection.Normalize();
        }
        else
        {
            Vector2 rand = Random.insideUnitCircle;
            if (rand == Vector2.zero) rand = Vector2.up;
            currentWindDirection = new Vector3(rand.x, 0, rand.y).normalized;
        }

        // Spawn a brand new rain instance and attach it to the camera (or player)
        if (rainPrefab != null)
        {
            Transform rainParent = Camera.main != null ? Camera.main.transform : playerTransform;
            activeRainInstance = Instantiate(rainPrefab, rainParent);
            
            // ADD THESE TWO LINES: Force the rain to center exactly on the parent camera
            activeRainInstance.transform.localPosition = Vector3.zero;
            activeRainInstance.transform.localRotation = Quaternion.identity;
        }

        yield return new WaitForSeconds(stormDuration);

        isStormActive = false;
        hasWarnedForStorm = false; 
        
        // Destroy the rain completely when the storm ends
        if (activeRainInstance != null)
        {
            Destroy(activeRainInstance);
        }
        
        nextStormTime = Time.time + timeBetweenStorms;
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
            warning.transform.localScale = new Vector3(blastRadius * 2f, 0.01f, blastRadius * 2f);
        }
        
        yield return new WaitForSeconds(warningDuration);
        
        if (!isStormActive)
        {
            if (warning != null) Destroy(warning);
            yield break;
        }

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
            pushDirection.y = 0.75f; 
            playerRb.AddForce(pushDirection.normalized * knockbackForce, ForceMode.Impulse);
        }
    }
}