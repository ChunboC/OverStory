using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CloudPlatformSpawner : MonoBehaviour
{
    [Header("Grid Layout")]
    public int rows = 5;
    public int columns = 5;
    public float spacingX = 20f;
    public float spacingZ = 20f;
    public float baseHeight = 0f;
    public float heightVariance = 2.5f;
    public int randomSeed = 42;

    [Header("Platform Size")]
    public float platformWidth = 12f;
    public float platformDepth = 12f;
    [Range(0.3f, 1f)]
    [Tooltip("Collider footprint as a fraction of the visual cloud size. Lower = player falls off closer to the edge.")]
    public float colliderScale = 0.5f;

    [Header("Cloud Puffs")]
    public int puffsPerPlatform = 5;
    public float puffHeightScale = 0.55f;

    [Header("Visuals")]
    public Material cloudMaterial;

#if UNITY_EDITOR
    [ContextMenu("Generate Cloud Platforms")]
    public void GeneratePlatforms()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);

        Random.InitState(randomSeed);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                float x = (c - (columns - 1) / 2f) * spacingX;
                float z = (r - (rows - 1) / 2f) * spacingZ;
                float y = baseHeight + Random.Range(-heightVariance, heightVariance);
                CreateCloudPlatform(new Vector3(x, y, z));
            }
        }

        Debug.Log($"Generated {rows * columns} cloud platforms under '{name}'. " +
                  "Select all children, mark as Navigation Static, then rebake the NavMesh.");
    }

    [ContextMenu("Clear Cloud Platforms")]
    public void ClearPlatforms()
    {
        while (transform.childCount > 0)
            DestroyImmediate(transform.GetChild(0).gameObject);
    }
#endif

    void CreateCloudPlatform(Vector3 worldPos)
    {
        GameObject platform = new GameObject("CloudPlatform");
        platform.transform.SetParent(transform, false);
        platform.transform.position = worldPos;

        // Thin slab sitting flush at y=0 — top surface IS the walking surface.
        // Puffs hang below so the visual cloud is under the player's feet.
        BoxCollider col = platform.AddComponent<BoxCollider>();
        col.size = new Vector3(platformWidth * colliderScale, 0.3f, platformDepth * colliderScale);
        col.center = Vector3.zero;

        // Central large puff: top of sphere touches y=0 so it looks like the cloud is right underfoot
        float centralRadius = platformWidth * 0.5f;
        AddPuff(platform, new Vector3(0f, -(centralRadius * puffHeightScale), 0f), centralRadius);

        // Ring of smaller puffs, each also anchored so their tops meet y=0
        for (int i = 0; i < puffsPerPlatform; i++)
        {
            float angle = (i / (float)puffsPerPlatform) * Mathf.PI * 2f;
            float dist = platformWidth * Random.Range(0.22f, 0.32f);
            float xOff = Mathf.Cos(angle) * dist;
            float zOff = Mathf.Sin(angle) * dist;
            float radius = platformWidth * Random.Range(0.18f, 0.30f);
            AddPuff(platform, new Vector3(xOff, -(radius * puffHeightScale), zOff), radius);
        }
    }

    void AddPuff(GameObject parent, Vector3 localPos, float radius)
    {
        GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);

#if UNITY_EDITOR
        DestroyImmediate(puff.GetComponent<SphereCollider>());
#else
        Destroy(puff.GetComponent<SphereCollider>());
#endif

        puff.transform.SetParent(parent.transform, false);
        puff.transform.localPosition = localPos;
        puff.transform.localScale = new Vector3(radius * 2f, radius * 2f * puffHeightScale, radius * 2f);

        if (cloudMaterial != null)
            puff.GetComponent<MeshRenderer>().sharedMaterial = cloudMaterial;
    }
}
