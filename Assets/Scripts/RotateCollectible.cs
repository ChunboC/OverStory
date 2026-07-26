using UnityEngine;

public class RotateCollectible : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(90f, 0f, 0f);

    public float hoverFrequency = 1f;
    public float hoverAmplitude = 0.2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);

        float newY = startPosition.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}