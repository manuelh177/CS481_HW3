using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerateRectangles : MonoBehaviour
{
    [Header("Sprites & Zones")]
    [SerializeField] private Sprite squareSprite; // Assign Unity's built-in 2D "Square" sprite
    [SerializeField] private BoxCollider2D startZone;
    [SerializeField] private BoxCollider2D endZone;

    [Header("Spawn Settings")]
    [SerializeField] private int obstacleCount = 20;
    [SerializeField] private float minWidth = 1f;
    [SerializeField] private float maxWidth = 2.5f;
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 3f;
    [SerializeField] private float safePadding = 0.2f;
    [SerializeField] private int maxPlacementAttempts = 300;

    [Header("Appearance")]
    [SerializeField] private Color rectangleColor = new Color(0.8f, 0.8f, 0.8f, 1f);
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

        int spawned = 0;

        for (int i = 0; i < obstacleCount; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float width = Random.Range(minWidth, maxWidth);
                float height = Random.Range(minHeight, maxHeight);
                Vector2 size = new Vector2(width, height);

                // Clamp position so the full rectangle stays inside spawn boundaries
                float halfW = width * 0.5f;
                float halfH = height * 0.5f;
                float randomX = Random.Range(spawnAreaMin.x + halfW, spawnAreaMax.x - halfW);
                float randomY = Random.Range(spawnAreaMin.y + halfH, spawnAreaMax.y - halfH);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                // Verify the candidate rectangle does not overlap Start or End zones
                if (IsOverlappingBox(candidatePos, size, startZone) || 
                    IsOverlappingBox(candidatePos, size, endZone))
                {
                    continue;
                }

                GameObject obstacle = CreateOutlinedRectangle(candidatePos, size, spawned);
                activeObstacles.Add(obstacle);

                placed = true;
                spawned++;
                break;
            }

            if (!placed)
            {
                Debug.LogWarning($"Could not place rectangle {i + 1} within {maxPlacementAttempts} attempts.");
            }
        }
    }

    private GameObject CreateOutlinedRectangle(Vector2 position, Vector2 size, int index)
    {
        GameObject root = new GameObject($"RectangleObstacle_{index}");
        root.transform.position = position;
        root.transform.parent = transform;

        Vector2 outlineSize = new Vector2(size.x + (outlineThickness * 2f), size.y + (outlineThickness * 2f));

        // 1. Black Outline (rendered behind the inner rectangle)
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.parent = root.transform;
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale = new Vector3(outlineSize.x, outlineSize.y, 1f);

        SpriteRenderer outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = squareSprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingOrder = baseSortingOrder;

        // 2. Inner Light Grey Rectangle
        GameObject innerObj = new GameObject("InnerRect");
        innerObj.transform.parent = root.transform;
        innerObj.transform.localPosition = Vector3.zero;
        innerObj.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer innerRenderer = innerObj.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = squareSprite;
        innerRenderer.color = rectangleColor;
        innerRenderer.sortingOrder = baseSortingOrder + 1;

        // 3. Collision box for 2D physics
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = size;

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

    /// <summary>
    /// AABB overlap test expanded by safePadding.
    /// </summary>
    private bool IsOverlappingBox(Vector2 center, Vector2 size, BoxCollider2D box)
    {
        if (box == null) return false;

        Bounds b = box.bounds;
        Vector2 halfExtents = (size * 0.5f) + new Vector2(safePadding, safePadding);

        bool overlapX = Mathf.Abs(center.x - b.center.x) <= (halfExtents.x + b.extents.x);
        bool overlapY = Mathf.Abs(center.y - b.center.y) <= (halfExtents.y + b.extents.y);

        return overlapX && overlapY;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 0f);
        Vector3 center = (spawnAreaMin + spawnAreaMax) * 0.5f;
        Gizmos.DrawWireCube(center, size);
    }
}
