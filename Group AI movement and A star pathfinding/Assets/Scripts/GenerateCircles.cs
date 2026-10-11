using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerateCircles : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private Grid pathfindingGrid;

    [Header("Sprites")]
    [SerializeField] private Sprite circleSprite;

    [Header("Spawn Settings")]
    [SerializeField] private int obstacleCount = 20;
    [SerializeField] private float minRadius = 0.5f;
    [SerializeField] private float maxRadius = 1.8f;

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

    [ContextMenu("Generate Obstacles")]
    public void GenerateObstacles()
    {
        ClearObstacles();

        for (int i = 0; i < obstacleCount; i++)
        {
            float radius = Random.Range(minRadius, maxRadius);
            float randomX = Random.Range(spawnAreaMin.x + radius, spawnAreaMax.x - radius);
            float randomY = Random.Range(spawnAreaMin.y + radius, spawnAreaMax.y - radius);
            Vector2 candidatePos = new Vector2(randomX, randomY);

            GameObject obstacle = CreateOutlinedCircle(candidatePos, radius, i);
            activeObstacles.Add(obstacle);
        }

        // Flush transform changes into the 3D physics scene before regenerating the grid
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

        // 3D Sphere Collider for Physics.CheckSphere detection
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
                // Disable immediately so the physics engine stops registering it before destruction
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 0f);
        Vector3 center = (spawnAreaMin + spawnAreaMax) * 0.5f;
        Gizmos.DrawWireCube(center, size);
    }
}