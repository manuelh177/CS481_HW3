using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GenerateRectangles : MonoBehaviour
{
    [Header("Grid Reference")]
    [SerializeField] private Grid pathfindingGrid;

    [Header("Sprites & Zones")]
    [SerializeField] private Sprite squareSprite;
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

    [Header("Layer & Appearance")]
    [SerializeField] private int obstacleLayer = 6; // Set to Layer 6 (Unwalkable)
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
            for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
            {
                float width = Random.Range(minWidth, maxWidth);
                float height = Random.Range(minHeight, maxHeight);
                Vector2 size = new Vector2(width, height);

                float halfW = width * 0.5f;
                float halfH = height * 0.5f;
                float randomX = Random.Range(spawnAreaMin.x + halfW, spawnAreaMax.x - halfW);
                float randomY = Random.Range(spawnAreaMin.y + halfH, spawnAreaMax.y - halfH);
                Vector2 candidatePos = new Vector2(randomX, randomY);

                if (IsOverlappingBox(candidatePos, size, startZone) || 
                    IsOverlappingBox(candidatePos, size, endZone))
                {
                    continue;
                }

                GameObject obstacle = CreateOutlinedRectangle(candidatePos, size, spawned);
                activeObstacles.Add(obstacle);
                spawned++;
                break;
            }
        }

        // Rebuild grid so Physics.CheckSphere picks up the new obstacles
        if (pathfindingGrid != null)
        {
            pathfindingGrid.CreateGrid();
        }
    }

    private GameObject CreateOutlinedRectangle(Vector2 position, Vector2 size, int index)
    {
        GameObject root = new GameObject($"RectangleObstacle_{index}");
        root.transform.position = new Vector3(position.x, position.y, 0f);
        root.transform.parent = transform;
        root.layer = obstacleLayer;

        Vector2 outlineSize = new Vector2(size.x + (outlineThickness * 2f), size.y + (outlineThickness * 2f));

        // Outline Sprite (Back)
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.parent = root.transform;
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localScale = new Vector3(outlineSize.x, outlineSize.y, 1f);
        SpriteRenderer outlineRenderer = outlineObj.AddComponent<SpriteRenderer>();
        outlineRenderer.sprite = squareSprite;
        outlineRenderer.color = outlineColor;
        outlineRenderer.sortingOrder = baseSortingOrder;

        // Inner Sprite (Front)
        GameObject innerObj = new GameObject("InnerRect");
        innerObj.transform.parent = root.transform;
        innerObj.transform.localPosition = Vector3.zero;
        innerObj.transform.localScale = new Vector3(size.x, size.y, 1f);
        SpriteRenderer innerRenderer = innerObj.AddComponent<SpriteRenderer>();
        innerRenderer.sprite = squareSprite;
        innerRenderer.color = rectangleColor;
        innerRenderer.sortingOrder = baseSortingOrder + 1;

        // 3D Box Collider for Physics.CheckSphere
        // Give Z a non-zero depth (e.g. 1f) so flat 2D points on Z=0 easily intersect it
        BoxCollider col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(outlineSize.x, outlineSize.y, 1f);

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

    private bool IsOverlappingBox(Vector2 center, Vector2 size, BoxCollider2D box)
    {
        if (box == null) return false;
        Bounds b = box.bounds;
        Vector2 halfExtents = (size * 0.5f) + new Vector2(safePadding, safePadding);
        bool overlapX = Mathf.Abs(center.x - b.center.x) <= (halfExtents.x + b.extents.x);
        bool overlapY = Mathf.Abs(center.y - b.center.y) <= (halfExtents.y + b.extents.y);
        return overlapX && overlapY;
    }
}