using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Editor-time placement tool for shamrock pickups, in the same spirit as
// CloudPlatformSpawner. Routes are authored as lists of platform positions and
// the tier decides how many shamrocks each stop is worth, so the harder line
// through a section pays out more than the safe one.
public class CollectibleSpawner : MonoBehaviour
{
    public enum RouteTier { Easy, Medium, Hard }

    [System.Serializable]
    public class CollectibleRoute
    {
        public string routeName = "Route";
        public RouteTier tier = RouteTier.Easy;
        public bool include = true;

        [Tooltip("World positions of the platforms along this route (platform transform position, not its surface).")]
        public Vector3[] platformPositions = new Vector3[0];
    }

    [Header("Prefab")]
    public GameObject collectiblePrefab;

    [Header("Placement")]
    [Tooltip("Height above the platform's transform position. Base_Cloud's walking surface sits ~0.47 above it.")]
    public float hoverHeight = 2f;
    [Tooltip("Spacing along X when a stop is worth more than one shamrock.")]
    public float clusterSpacing = 0.9f;

    [Header("Reward Per Stop")]
    public int easyReward = 1;
    public int mediumReward = 2;
    public int hardReward = 3;

    [Header("Routes")]
    public List<CollectibleRoute> routes = new List<CollectibleRoute>();

    public int RewardFor(RouteTier tier)
    {
        switch (tier)
        {
            case RouteTier.Hard: return hardReward;
            case RouteTier.Medium: return mediumReward;
            default: return easyReward;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Collectibles")]
    public void GenerateCollectibles()
    {
        if (collectiblePrefab == null)
        {
            Debug.LogError("[CollectibleSpawner] No collectible prefab assigned.", this);
            return;
        }

        ClearCollectibles();

        int total = 0;

        foreach (CollectibleRoute route in routes)
        {
            if (!route.include || route.platformPositions == null) continue;

            int reward = RewardFor(route.tier);

            foreach (Vector3 platformPosition in route.platformPositions)
            {
                for (int i = 0; i < reward; i++)
                {
                    // Centre the cluster on the platform and fan it out along X.
                    float offsetX = (i - (reward - 1) / 2f) * clusterSpacing;

                    Vector3 spawnPosition = platformPosition
                        + new Vector3(offsetX, hoverHeight, 0f);

                    GameObject collectible =
                        (GameObject)PrefabUtility.InstantiatePrefab(collectiblePrefab, transform);

                    collectible.name = $"Shamrock_{route.routeName}_{total:D3}";
                    collectible.transform.position = spawnPosition;

                    Undo.RegisterCreatedObjectUndo(collectible, "Generate Collectibles");
                    total++;
                }
            }
        }

        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

        Debug.Log($"[CollectibleSpawner] Placed {total} shamrock(s) across {routes.Count} route(s) under '{name}'.");
    }

    [ContextMenu("Clear Collectibles")]
    public void ClearCollectibles()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    // Seeded from the platform layout that is actually in Level1_Scene, so the
    // tool is usable straight out of the box. Re-run after moving platforms.
    [ContextMenu("Load Level 1 Default Routes")]
    public void LoadLevel1DefaultRoutes()
    {
        routes = new List<CollectibleRoute>
        {
            // --- First fork (X 24-60): three ways past the opening hub ---
            new CollectibleRoute
            {
                routeName = "Fork_North_Ramp",
                tier = RouteTier.Hard,
                platformPositions = new[]
                {
                    new Vector3(36f, 2.52f, -11f),
                    new Vector3(43f, 3.32f, -11f),
                    new Vector3(51f, 3.32f, -11f),
                    // The 9-unit gap down onto the last small cloud - the
                    // hardest single jump in the opening section.
                    new Vector3(60f, 0f, -12f),
                }
            },
            new CollectibleRoute
            {
                routeName = "Fork_Mid_MovingClouds",
                tier = RouteTier.Medium,
                platformPositions = new[]
                {
                    new Vector3(30f, 0f, 0f),
                    new Vector3(36f, 0f, 0f),
                    new Vector3(42f, 0f, 0f),
                    new Vector3(48f, 0f, 0f),
                    new Vector3(54f, 0f, 0f),
                }
            },
            new CollectibleRoute
            {
                routeName = "Fork_South_Elevator",
                tier = RouteTier.Easy,
                platformPositions = new[]
                {
                    new Vector3(32f, 0f, 15f),
                    new Vector3(45f, 0f, 15f),
                    new Vector3(58f, 0f, 15f),
                }
            },

            // --- Middle stretch (X 94-142): low bridge line vs upper ledge ---
            new CollectibleRoute
            {
                routeName = "Mid_LowBridge",
                tier = RouteTier.Easy,
                platformPositions = new[]
                {
                    new Vector3(102f, 0f, 2f),
                    new Vector3(112f, 0.5f, 2f),
                    new Vector3(126f, 0f, 2f),
                }
            },
            new CollectibleRoute
            {
                routeName = "Mid_UpperLedge",
                tier = RouteTier.Medium,
                platformPositions = new[]
                {
                    new Vector3(102f, 0f, 15f),
                    new Vector3(114f, 0f, 15f),
                    new Vector3(120f, 0f, 15f),
                    new Vector3(126f, 0f, 15f),
                }
            },

            // --- The loop (X 146-192): around the top, around the bottom, or
            //     straight over the elevated ramp in the middle ---
            new CollectibleRoute
            {
                routeName = "Loop_Top",
                tier = RouteTier.Medium,
                platformPositions = new[]
                {
                    new Vector3(154f, 0f, -19f),
                    new Vector3(167f, 0f, -19f),
                    new Vector3(184f, 0f, -19f),
                }
            },
            new CollectibleRoute
            {
                routeName = "Loop_Bottom",
                tier = RouteTier.Medium,
                platformPositions = new[]
                {
                    new Vector3(154f, 0f, 27f),
                    new Vector3(169f, 0f, 27f),
                    new Vector3(184f, 0f, 27f),
                }
            },
            new CollectibleRoute
            {
                routeName = "Loop_Elevated",
                tier = RouteTier.Hard,
                platformPositions = new[]
                {
                    new Vector3(156f, 4.14f, 6f),
                    new Vector3(162f, 7.38f, 6f),
                    new Vector3(170f, 8.18f, 6f),
                    new Vector3(182f, 8.18f, 6f),
                }
            },

            // --- Final run to the beanstalk ---
            new CollectibleRoute
            {
                routeName = "Final_Run",
                tier = RouteTier.Easy,
                platformPositions = new[]
                {
                    new Vector3(204f, 0f, 0f),
                    new Vector3(214f, 0.5f, -1f),
                    new Vector3(228f, 0f, 0f),
                }
            },
        };

        EditorUtility.SetDirty(this);
        Debug.Log($"[CollectibleSpawner] Loaded {routes.Count} default Level 1 routes ({TotalPlannedShamrocks()} shamrocks when generated).");
    }
#endif

    public int TotalPlannedShamrocks()
    {
        int total = 0;

        foreach (CollectibleRoute route in routes)
        {
            if (!route.include || route.platformPositions == null) continue;
            total += route.platformPositions.Length * RewardFor(route.tier);
        }

        return total;
    }

    private void OnDrawGizmos()
    {
        foreach (CollectibleRoute route in routes)
        {
            if (!route.include || route.platformPositions == null) continue;

            switch (route.tier)
            {
                case RouteTier.Hard: Gizmos.color = Color.red; break;
                case RouteTier.Medium: Gizmos.color = Color.yellow; break;
                default: Gizmos.color = Color.green; break;
            }

            for (int i = 0; i < route.platformPositions.Length; i++)
            {
                Vector3 point = route.platformPositions[i] + Vector3.up * hoverHeight;

                Gizmos.DrawWireSphere(point, 0.6f);

                if (i > 0)
                {
                    Gizmos.DrawLine(
                        route.platformPositions[i - 1] + Vector3.up * hoverHeight,
                        point
                    );
                }
            }
        }
    }
}
