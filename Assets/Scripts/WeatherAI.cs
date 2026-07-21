using UnityEngine;
using System.Collections;

public class WeatherAI : MonoBehaviour
{
    [Header("Targeting & Prediction")]
    public Transform playerTransform;
    public Rigidbody playerRb; 
    
    [Header("Storm Settings (Occasional)")]
    public float timeBetweenStorms = 18f; // Happens slightly more frequently
    public float stormDuration = 3.5f;    // Shortened to just a couple seconds
    public float stormWarningTime = 3f; 
    
    public GameObject rainEffect; 
    public float windForce = 3f; 
    
    private bool isStormActive = false;
    private bool hasWarnedForStorm = false;
    private float nextStormTime;
    private Vector3 currentWindDirection;

    [Header("Lightning Settings (Constant Random)")]
    public GameObject lightningWarningPrefab; 
    public GameObject lightningStrikePrefab;  
    private float minStrikeCooldown = 4.0f; 
    private float maxStrikeCooldown = 6.0f;
    private float nextStrikeTime;
    private float warningDuration = 1.2f; 
    private float blastRadius = 4.5f;
    public float knockbackForce = 45f;

    [Header("Escalation Timer")]
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
        nextStormTime = Time.time + timeBetweenStorms;
        nextStrikeTime = Time.time + Random.Range(minStrikeCooldown, maxStrikeCooldown);

        if (rainEffect != null) rainEffect.SetActive(false);
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;
        UpdateDifficultyPhase();

        if (Time.time >= nextStrikeTime && playerTransform != null)
        {
            StartCoroutine(ExecuteLightningStrike());
            nextStrikeTime = Time.time + Random.Range(minStrikeCooldown, maxStrikeCooldown);
        }

        if (!isStormActive && !hasWarnedForStorm && Time.time >= (nextStormTime - stormWarningTime))
        {
            Debug.Log("<color=orange>[WEATHER AI]</color> rainstorm with strong winds incoming");
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
            playerRb.AddForce(currentWindDirection * windForce, ForceMode.Force);
        }
    }

    private IEnumerator ExecuteStormEvent()
    {
        isStormActive = true;
        Debug.Log("<color=cyan>[WEATHER AI]</color> Storm Started! Rain and Wind active.");

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

        if (rainEffect != null) rainEffect.SetActive(true);

        yield return new WaitForSeconds(stormDuration);

        isStormActive = false;
        hasWarnedForStorm = false; 
        
        if (rainEffect != null) rainEffect.SetActive(false);
        
        nextStormTime = Time.time + timeBetweenStorms;
        Debug.Log("<color=cyan>[WEATHER AI]</color> Storm Ended. Weather clear.");
    }

    private void UpdateDifficultyPhase()
    {
        if (timeElapsed >= 40f && currentPhase < 3)
        {
            currentPhase = 3;
            minStrikeCooldown = 2.5f; 
            maxStrikeCooldown = 4.0f;
            warningDuration = 0.85f; 
            blastRadius = 6.0f; 
            windForce = 5f; 
        }
        else if (timeElapsed >= 20f && timeElapsed < 40f && currentPhase < 2)
        {
            currentPhase = 2;
            minStrikeCooldown = 3.0f;
            maxStrikeCooldown = 5.0f;
            warningDuration = 1.0f;
            blastRadius = 5.0f; 
            windForce = 4f; 
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
            pushDirection.y = 0.75f; 
            playerRb.AddForce(pushDirection.normalized * knockbackForce, ForceMode.Impulse);
        }
    }
}