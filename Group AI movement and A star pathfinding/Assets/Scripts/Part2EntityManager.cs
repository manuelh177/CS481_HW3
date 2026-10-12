using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Part2EntityManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private BoxCollider2D startZone;
    [SerializeField] private BoxCollider2D endZone;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Sprite entitySprite; // Arrow, triangle, or square sprite

    [Header("Settings")]
    [SerializeField] private int entityCount = 5;
    [SerializeField] private float entitySize = 0.4f;
    [SerializeField] private Color defaultColor = new Color(0.2f, 0.7f, 1f); // Cyan
    [SerializeField] private Color selectedColor = Color.magenta;

    private readonly List<Part2EntityUnit> entities = new List<Part2EntityUnit>();
    private Part2EntityUnit currentSelectedEntity;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        SpawnEntities();
    }

    private void Update()
    {
        HandleSelection();
        HandleTargetOrder();

        // Regenerate entities with 'C'
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            SpawnEntities();
        }
    }

    public void SpawnEntities()
    {
        // Clear previous entities
        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i] != null) Destroy(entities[i].gameObject);
        }
        entities.Clear();
        currentSelectedEntity = null;

        if (startZone == null) return;

        Bounds bounds = startZone.bounds;
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents; // Half-width and half-height of the box

        // Keep entities slightly inside the border (using 75% - 85% of the box's half-size)
        float paddingFactor = 0.8f; 
        float spanX = extents.x * paddingFactor;
        float spanY = extents.y * paddingFactor;

        for (int i = 0; i < entityCount; i++)
        {
            // Pick a random point distributed throughout the entire area of the box
            float randomX = Random.Range(center.x - spanX, center.x + spanX);
            float randomY = Random.Range(center.y - spanY, center.y + spanY);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0f);

            // Random orientation (0 to 360 degrees)
            Quaternion randomRot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            // Create Entity GameObject
            GameObject entityObj = new GameObject($"Entity_{i + 1}");
            entityObj.transform.position = spawnPos;
            entityObj.transform.rotation = randomRot;
            entityObj.transform.localScale = Vector3.one * entitySize;

            // Visual
            SpriteRenderer sr = entityObj.AddComponent<SpriteRenderer>();
            sr.sprite = entitySprite;
            sr.sortingOrder = 15;

            // 2D Collider for mouse clicking/selection
            CircleCollider2D col = entityObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            // Controller component
            Part2EntityUnit unit = entityObj.AddComponent<Part2EntityUnit>();
            unit.Initialize(defaultColor, selectedColor);

            entities.Add(unit);
        }
    }

    private void HandleSelection()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));

        // Raycast to check if an entity was clicked
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            Part2EntityUnit clickedUnit = hit.collider.GetComponent<Part2EntityUnit>();
            if (clickedUnit != null)
            {
                // Deselect previous
                if (currentSelectedEntity != null)
                {
                    currentSelectedEntity.SetSelected(false);
                }

                currentSelectedEntity = clickedUnit;
                currentSelectedEntity.SetSelected(true);
            }
        }
    }

    private void HandleTargetOrder()
    {
        if (Mouse.current == null || !Mouse.current.rightButton.wasPressedThisFrame) return;
        if (currentSelectedEntity == null || endZone == null || pathfinding == null) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 clickWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        clickWorldPos.z = 0f;

        // Verify the right-click is strictly within the target/end area
        if (endZone.bounds.Contains(clickWorldPos))
        {
            Vector3[] waypoints = pathfinding.FindPathWaypoints(currentSelectedEntity.transform.position, clickWorldPos);
            if (waypoints != null && waypoints.Length > 0)
            {
                currentSelectedEntity.FollowPath(waypoints);
            }
        }
    }
}
