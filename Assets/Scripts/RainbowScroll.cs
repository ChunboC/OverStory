using UnityEngine;
using System.Collections.Generic;

// Scrolls the rainbow texture along the ramps for an animated, flowing look.
// Drop this on an individual ramp or on the parent "Ramps" object — it picks up
// this Renderer plus any child Renderers. Uses instanced materials, so the shared
// Ramp.mat asset is never modified.
public class RainbowScroll : MonoBehaviour
{
    [Header("Scroll Settings")]
    [Tooltip("Texture units per second. Positive scrolls one way, negative the other.")]
    public float scrollSpeed = 0.25f;

    [Tooltip("Scroll along U (across the rainbow bands). Turn off to scroll V instead.")]
    public bool scrollU = true;

    [Tooltip("Also animate the emission map so the glow flows with the color.")]
    public bool scrollEmission = true;

    private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");

    private readonly List<Material> materials = new List<Material>();

    void Start()
    {
        // Collect instanced materials from this object and all children.
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials) // .materials returns instances
            {
                materials.Add(m);
            }
        }
    }

    void Update()
    {
        float scroll = Time.time * scrollSpeed;
        Vector2 offset = scrollU ? new Vector2(scroll, 0f) : new Vector2(0f, scroll);

        foreach (Material m in materials)
        {
            if (m.HasProperty(BaseMap)) m.SetTextureOffset(BaseMap, offset);
            if (scrollEmission && m.HasProperty(EmissionMap)) m.SetTextureOffset(EmissionMap, offset);
        }
    }
}
