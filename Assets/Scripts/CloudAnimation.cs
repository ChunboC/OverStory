using UnityEngine;

public class CloudAnimation : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMesh;
    private float timer;

    void Start()
    {
        skinnedMesh = GetComponent<SkinnedMeshRenderer>();
    }

    void Update()
    {
        if (skinnedMesh == null || skinnedMesh.sharedMesh == null) return;

        int totalShapes = skinnedMesh.sharedMesh.blendShapeCount;
        if (totalShapes == 0) return;

        timer += Time.deltaTime * 5f;

        for (int i = 0; i < totalShapes; i++)
        {
            float offset = i * 1.5f;
            float weight = (Mathf.Sin(timer + offset) + 1f) * 30f;

            skinnedMesh.SetBlendShapeWeight(i, weight);
        }
    }
}
