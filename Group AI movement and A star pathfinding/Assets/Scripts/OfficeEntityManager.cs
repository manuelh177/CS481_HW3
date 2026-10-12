using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class OfficeEntityManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
    [SerializeField] private BoxCollider2D startZone;
    [SerializeField] private BoxCollider2D endZone;
    [SerializeField] private Camera mainCamera;

    [Header("Office Centers (Green Capsules)")]
    [Tooltip("Drag the green capsule transforms here, or leave empty to auto-detect by tag.")]
    [SerializeField] private List<Transform> officeCenters = new List<Transform>();
    [SerializeField] private string officeTag = "Office";

    [Header("Entities Settings")]
    [SerializeField] private Sprite entitySprite;
    [SerializeField] private int entityCount = 5;
    [SerializeField] private float entitySize = 0.4f;
    [SerializeField] private Color defaultColor = new Color(0.2f, 0.7f, 1f);
    [SerializeField] private Color selectedColor = Color.magenta;

    [Header("Visual Path Settings")]
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Color officePointColor = Color.green;
    [SerializeField] private Color pathColor = Color.blue;
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private int visualSortingOrder = 10;

    private readonly List<Part2EntityUnit> entities = new List<Part2EntityUnit>();
    private readonly List<GameObject> pathTiles = new List<GameObject>();
    private Part2EntityUnit currentSelectedEntity;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Auto-detect green capsules if list is empty
        if (officeCenters == null || officeCenters.Count == 0)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(officeTag);
            foreach (GameObject obj in found)
            {
                officeCenters.Add(obj.transform);
            }
        }
    }

    private void Start()
    {
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
            ClearPathTiles();
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
        Vector3 extents = bounds.extents;

        float paddingFactor = 0.8f;
        float spanX = extents.x * paddingFactor;
        float spanY = extents.y * paddingFactor;

        for (int i = 0; i < entityCount; i++)
        {
            float randomX = Random.Range(center.x - spanX, center.x + spanX);
            float randomY = Random.Range(center.y - spanY, center.y + spanY);
            Vector3 spawnPos = new Vector3(randomX, randomY, 0f);

            Quaternion randomRot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            GameObject entityObj = new GameObject($"OfficeEntity_{i + 1}");
            entityObj.transform.position = spawnPos;
            entityObj.transform.rotation = randomRot;
            entityObj.transform.localScale = Vector3.one * entitySize;

            SpriteRenderer sr = entityObj.AddComponent<SpriteRenderer>();
            sr.sprite = entitySprite;
            sr.sortingOrder = 15;

            CircleCollider2D col = entityObj.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

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

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            Part2EntityUnit clickedUnit = hit.collider.GetComponent<Part2EntityUnit>();
            if (clickedUnit != null)
            {
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
        if (currentSelectedEntity == null || endZone == null || pathfinding == null || officeCenters.Count == 0) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 clickWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        clickWorldPos.z = 0f;

        // Check if right click was inside the End/Target area
        if (endZone.bounds.Contains(clickWorldPos))
        {
            List<Vector3> fullTourWaypoints = BuildTourWithFinalTarget(currentSelectedEntity.transform.position, clickWorldPos, out List<Vector3> orderedOffices);

            if (fullTourWaypoints != null && fullTourWaypoints.Count > 0)
            {
                DrawTourPath(fullTourWaypoints, orderedOffices, clickWorldPos);
                currentSelectedEntity.FollowPath(fullTourWaypoints.ToArray());
            }
        }
    }

    /// <summary>
    /// Builds an A* path from the entity's position -> closest office -> all adjacent offices -> final target area point.
    /// </summary>
    private List<Vector3> BuildTourWithFinalTarget(Vector3 startPoint, Vector3 finalTarget, out List<Vector3> orderedOffices)
    {
        orderedOffices = OrderOfficesAdjacently(startPoint);
        List<Vector3> combinedWaypoints = new List<Vector3>();
        Vector3 currentPos = startPoint;

        // 1. Traverse all adjacent offices
        for (int i = 0; i < orderedOffices.Count; i++)
        {
            Vector3 nextOffice = orderedOffices[i];
            Vector3[] segment = pathfinding.FindPathWaypoints(currentPos, nextOffice);

            if (segment != null && segment.Length > 0)
            {
                int startIndex = (combinedWaypoints.Count > 0) ? 1 : 0;
                for (int s = startIndex; s < segment.Length; s++)
                {
                    combinedWaypoints.Add(segment[s]);
                }
                currentPos = nextOffice;
            }
        }

        // 2. From the last visited office, navigate to the target area point
        Vector3[] finalSegment = pathfinding.FindPathWaypoints(currentPos, finalTarget);
        if (finalSegment != null && finalSegment.Length > 0)
        {
            int startIndex = (combinedWaypoints.Count > 0) ? 1 : 0;
            for (int s = startIndex; s < finalSegment.Length; s++)
            {
                combinedWaypoints.Add(finalSegment[s]);
            }
        }

        return combinedWaypoints;
    }

    private List<Vector3> OrderOfficesAdjacently(Vector3 startPoint)
    {
        Transform nearestOffice = officeCenters
            .OrderBy(o => Vector2.Distance(startPoint, o.position))
            .First();

        float avgY = officeCenters.Average(o => o.position.y);
        List<Transform> topRow = officeCenters.Where(o => o.position.y >= avgY).OrderBy(o => o.position.x).ToList();
        List<Transform> bottomRow = officeCenters.Where(o => o.position.y < avgY).OrderBy(o => o.position.x).ToList();

        bool startInTop = nearestOffice.position.y >= avgY;
        List<Transform> ordered = new List<Transform>();

        if (startInTop)
        {
            int index = topRow.IndexOf(nearestOffice);
            if (index <= topRow.Count / 2)
            {
                ordered.AddRange(topRow.Skip(index));
                if (index > 0) ordered.InsertRange(0, topRow.Take(index).Reverse());
                bottomRow.Reverse();
                ordered.AddRange(bottomRow);
            }
            else
            {
                List<Transform> leftSlice = topRow.Take(index + 1).Reverse().ToList();
                ordered.AddRange(leftSlice);
                ordered.AddRange(bottomRow);
                ordered.AddRange(topRow.Skip(index + 1));
            }
        }
        else
        {
            int index = bottomRow.IndexOf(nearestOffice);
            if (index <= bottomRow.Count / 2)
            {
                ordered.AddRange(bottomRow.Skip(index));
                if (index > 0) ordered.InsertRange(0, bottomRow.Take(index).Reverse());
                topRow.Reverse();
                ordered.AddRange(topRow);
            }
            else
            {
                List<Transform> leftSlice = bottomRow.Take(index + 1).Reverse().ToList();
                ordered.AddRange(leftSlice);
                ordered.AddRange(topRow);
                ordered.AddRange(bottomRow.Skip(index + 1));
            }
        }

        return ordered.Select(t => new Vector3(t.position.x, t.position.y, 0f)).ToList();
    }

    private void DrawTourPath(List<Vector3> waypoints, List<Vector3> offices, Vector3 finalTarget)
    {
        ClearPathTiles();

        float tileSize = grid.nodeRadius * 2f;
        HashSet<Vector2> officeSet = new HashSet<Vector2>(offices.Select(p => new Vector2(Mathf.Round(p.x * 100f) / 100f, Mathf.Round(p.y * 100f) / 100f)));
        Vector2 targetRounded = new Vector2(Mathf.Round(finalTarget.x * 100f) / 100f, Mathf.Round(finalTarget.y * 100f) / 100f);

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 pos = waypoints[i];
            Vector2 roundPos = new Vector2(Mathf.Round(pos.x * 100f) / 100f, Mathf.Round(pos.y * 100f) / 100f);

            GameObject tile;
            if (i < pathTiles.Count)
            {
                tile = pathTiles[i];
                tile.SetActive(true);
            }
            else
            {
                tile = new GameObject($"TourTile_{i}");
                tile.transform.parent = transform;
                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = squareSprite;
                pathTiles.Add(tile);
            }

            tile.transform.position = pos;
            tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();

            if (roundPos == targetRounded || i == waypoints.Count - 1)
            {
                renderer.color = targetColor;
                renderer.sortingOrder = visualSortingOrder + 2;
            }
            else if (officeSet.Contains(roundPos))
            {
                renderer.color = officePointColor;
                renderer.sortingOrder = visualSortingOrder + 1;
            }
            else
            {
                renderer.color = pathColor;
                renderer.sortingOrder = visualSortingOrder;
            }
        }
    }

    private void ClearPathTiles()
    {
        for (int i = 0; i < pathTiles.Count; i++)
        {
            pathTiles[i].SetActive(false);
        }
    }
}
