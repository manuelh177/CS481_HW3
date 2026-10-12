using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerateCircles : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private Grid pathfindingGrid;

    [Header("Sprites & Zones")]
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private BoxCollider2D startZone;
    [SerializeField] private BoxCollider2D endZone;

    [Header("Spawn Settings")]
    [SerializeField] private int obstacleCount = 20;
    [SerializeField] private float minRadius = 0.5f;
    [SerializeField] private float maxRadius = 1.8f;
    [SerializeField] private float safePadding = 0.2f;
    [SerializeField] private int maxPlacementAttempts = 300;

    [Header("Layer & Appearance")]
    [SerializeField] private int obstacleLayer = 6; // Layer 6 (Unwalkable)
    [SerializeField] private Color circleColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color outlineColor = Color.black;
    [SerializeField] private float outlineThickness = 0.05f;
    [SerializeField] private int baseSortingOrder = 1;

    [Header("Spawn Boundaries")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-8f, -4.5f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(8f, 4.5f);

    private readonly List<GameObject> activeObstacles = new List<GameObject>();

    private void Start()
    {
        GenerateObstacles();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            GenerateObstacles();
        }
    }

    public void SetObstacleCount(int count)
    {
        obstacleCount = count;
    }

    [ContextMenu("Generate Obstacles")]
    public void GenerateObstacles()
    {
        ClearObstacles();

        int spawned = 0;

        for (int i = 0; i < obstacleCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float radius = Random.Range(minRadius, maxRadius);
                float randomX = Random.Range(spawnAreaMin.x + radius, spawnAreaMax.x - radius);
                float randomY = Random.Range(spawnAreaMin.y + radius, spawnAreaMax.y - radius);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                // Prevent overlap with Start or End zones
                if (IsOverlappingBox(candidatePos, radius, startZone) || 
                    IsOverlappingBox(candidatePos, radius, endZone))
                {
                    continue;
                }

                GameObject obstacle = CreateOutlinedCircle(candidatePos, radius, spawned);
                activeObstacles.Add(obstacle);
                placed = true;
                spawned++;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"Could not find a valid spot for circle {i + 1} after {maxPlacementAttempts} attempts.");
            }
        }

        // Synchronize transform changes with the 3D physics tree
        Physics.SyncTransforms();

        if (pathfindingGrid != null)
        {
            pathfindingGrid.CreateGrid();
        }
    }

    private GameObject CreateOutlinedCircle(Vector2 position, float radius, int index)
    {
        GameObject root = new GameObject($"CircleObstacle_{index}");
        root.transform.position = new Vector3(position.x, position.y, 0f);
        root.transform.parent = transform;
        root.layer = obstacleLayer;

        float diameter = radius * 2f;
        float totalRadius = radius + outlineThickness;
        float outlineDiameter = totalRadius * 2f;

        // Outline Sprite (Back)
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.parent = root.transform;
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale = new Vector3(outlineDiameter, outlineDiameter, 1f);
        SpriteRenderer outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = circleSprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingOrder = baseSortingOrder;

        // Inner Sprite (Front)
        GameObject innerObj = new GameObject("InnerCircle");
        innerObj.transform.parent = root.transform;
        innerObj.transform.localPosition = Vector3.zero;
        innerObj.transform.localScale = new Vector3(diameter, diameter, 1f);
        SpriteRenderer innerRenderer = innerObj.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = circleSprite;
        innerRenderer.color = circleColor;
        innerRenderer.sortingOrder = baseSortingOrder + 1;

        // 3D Sphere Collider for Physics.CheckSphere
        SphereCollider col = root.AddComponent<SphereCollider>();
        col.radius = totalRadius;

        return root;
    }

    public void ClearObstacles()
    {
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            if (activeObstacles[i] != null)
            {
                Collider col = activeObstacles[i].GetComponent<Collider>();
                if (col != null)
                {
                    col.enabled = false;
                }

                activeObstacles[i].SetActive(false);

                if (Application.isPlaying)
                    Destroy(activeObstacles[i]);
                else
                    DestroyImmediate(activeObstacles[i]);
            }
        }
        activeObstacles.Clear();
    }

    private bool IsOverlappingBox(Vector2 circleCenter, float circleRadius, BoxCollider2D box)
    {
        if (box == null) return false;

        Bounds bounds = box.bounds;
        float closestX = Mathf.Clamp(circleCenter.x, bounds.min.x, bounds.max.x);
        float closestY = Mathf.Clamp(circleCenter.y, bounds.min.y, bounds.max.y);
        Vector2 closestPoint = new Vector2(closestX, closestY);

        float distance = Vector2.Distance(circleCenter, closestPoint);
        return distance < (circleRadius + safePadding);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 0f);
        Vector3 center = (spawnAreaMin + spawnAreaMax) * 0.5f;
        Gizmos.DrawWireCube(center, size);
    }
}