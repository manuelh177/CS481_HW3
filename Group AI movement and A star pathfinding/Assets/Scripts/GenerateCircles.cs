using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Required for the new Input System

public class GenerateCircles : MonoBehaviour
{
    [Header("Prefabs & Spawn Targets")]
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private BoxCollider2D startZone;
    [SerializeField] private BoxCollider2D endZone;

    [Header("Spawn Settings")]
    [SerializeField] private int obstacleCount = 20;
    [SerializeField] private float minRadius = 0.5f;
    [SerializeField] private float maxRadius = 1.8f;
    [SerializeField] private float safePadding = 0.2f;
    [SerializeField] private int maxPlacementAttempts = 300;

    [Header("Appearance")]
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
        // New Input System check for the 'C' key
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            GenerateObstacles();
        }
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
    }

    private GameObject CreateOutlinedCircle(Vector2 position, float radius, int index)
    {
        GameObject root = new GameObject($"CircleObstacle_{index}");
        root.transform.position = position;
        root.transform.parent = transform;

        float diameter = radius * 2f;
        float outlineDiameter = (radius + outlineThickness) * 2f;

        // 1. Outline Circle
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.parent = root.transform;
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale = new Vector3(outlineDiameter, outlineDiameter, 1f);

        SpriteRenderer outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = circleSprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingOrder = baseSortingOrder;

        // 2. Inner Main Circle
        GameObject innerObj = new GameObject("InnerCircle");
        innerObj.transform.parent = root.transform;
        innerObj.transform.localPosition = Vector3.zero;
        innerObj.transform.localScale = new Vector3(diameter, diameter, 1f);

        SpriteRenderer innerRenderer = innerObj.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = circleSprite;
        innerRenderer.color = circleColor;
        innerRenderer.sortingOrder = baseSortingOrder + 1;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = radius;

        return root;
    }

    public void ClearObstacles()
    {
        for (int i = 0; i < activeObstacles.Count; i++)
        {
            if (activeObstacles[i] != null)
            {
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