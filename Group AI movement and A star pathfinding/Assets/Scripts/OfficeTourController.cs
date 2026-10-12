using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class OfficeTourController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
    [SerializeField] private Camera mainCamera;

    [Header("Office Centers (Green Capsules)")]
    [Tooltip("Drag the green capsule transforms here, or leave empty to auto-detect by tag.")]
    [SerializeField] private List<Transform> officeCenters = new List<Transform>();
    [SerializeField] private string officeTag = "Office";

    [Header("Visual Display")]
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Color startClickColor = Color.yellow;
    [SerializeField] private Color officeNodeColor = Color.green;
    [SerializeField] private Color pathColor = Color.blue;
    [SerializeField] private int sortingOrder = 10;

    private readonly List<GameObject> pathTiles = new List<GameObject>();
    private GameObject clickMarker;
    private List<Vector3> fullTourWaypoints = new List<Vector3>();

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        // Auto-find office points if not manually assigned in inspector
        if (officeCenters == null || officeCenters.Count == 0)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(officeTag);
            foreach (GameObject obj in found)
            {
                officeCenters.Add(obj.transform);
            }
        }
    }

    private void Update()
    {
        // Left click to start tour generation from clicked point
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }

        // 'C' resets the tour path
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            ResetTour();
        }
    }

    private void HandleLeftClick()
    {
        if (grid == null || pathfinding == null || officeCenters.Count == 0) return;

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        worldPos.z = 0f;

        Node clickedNode = grid.NodeFromWorldPoint(worldPos);
        if (clickedNode == null || !clickedNode.walkable) return;

        GenerateFullTour(new Vector3(clickedNode.worldPos.x, clickedNode.worldPos.y, 0f));
    }

    private void GenerateFullTour(Vector3 startPoint)
    {
        ResetTour();

        // 1. Mark initial click location
        SetMarker(ref clickMarker, "StartClickMarker", startPoint, startClickColor);

        // 2. Build ordered visit sequence using Adjacent Office traversal
        List<Vector3> visitOrder = OrderOfficesAdjacently(startPoint);

        // 3. Chain A* path segments together
        fullTourWaypoints.Clear();
        Vector3 currentPos = startPoint;

        for (int i = 0; i < visitOrder.Count; i++)
        {
            Vector3 nextTarget = visitOrder[i];
            Vector3[] segment = pathfinding.FindPathWaypoints(currentPos, nextTarget);

            if (segment != null && segment.Length > 0)
            {
                // Skip the first waypoint on subsequent segments to avoid duplicate joints
                int startIndex = (fullTourWaypoints.Count > 0) ? 1 : 0;
                for (int s = startIndex; s < segment.Length; s++)
                {
                    fullTourWaypoints.Add(segment[s]);
                }
                currentPos = nextTarget;
            }
        }

        // 4. Draw path tiles in the scene
        DrawPathTiles(visitOrder);
    }

    /// <summary>
    /// Divides offices into top and bottom rows based on Y coordinates,
    /// then traverses one row and loops through the other row.
    /// </summary>
    private List<Vector3> OrderOfficesAdjacently(Vector3 startPoint)
    {
        // Find closest office to start position
        Transform nearestOffice = officeCenters
            .OrderBy(o => Vector2.Distance(startPoint, o.position))
            .First();

        // Calculate average Y to split into top and bottom rows
        float avgY = officeCenters.Average(o => o.position.y);
        List<Transform> topRow = officeCenters.Where(o => o.position.y >= avgY).OrderBy(o => o.position.x).ToList();
        List<Transform> bottomRow = officeCenters.Where(o => o.position.y < avgY).OrderBy(o => o.position.x).ToList();

        bool startInTop = nearestOffice.position.y >= avgY;
        List<Transform> orderedOffices = new List<Transform>();

        if (startInTop)
        {
            int index = topRow.IndexOf(nearestOffice);
            // Decide direction: moving right or moving left across top row
            if (index <= topRow.Count / 2)
            {
                // Sweep right on top, then right-to-left on bottom
                orderedOffices.AddRange(topRow.Skip(index));
                if (index > 0) orderedOffices.InsertRange(0, topRow.Take(index).Reverse());
                bottomRow.Reverse();
                orderedOffices.AddRange(bottomRow);
            }
            else
            {
                // Sweep left on top, then left-to-right on bottom
                List<Transform> leftSlice = topRow.Take(index + 1).Reverse().ToList();
                orderedOffices.AddRange(leftSlice);
                orderedOffices.AddRange(bottomRow);
                orderedOffices.AddRange(topRow.Skip(index + 1));
            }
        }
        else
        {
            int index = bottomRow.IndexOf(nearestOffice);
            if (index <= bottomRow.Count / 2)
            {
                // Sweep right on bottom, then right-to-left on top
                orderedOffices.AddRange(bottomRow.Skip(index));
                if (index > 0) orderedOffices.InsertRange(0, bottomRow.Take(index).Reverse());
                topRow.Reverse();
                orderedOffices.AddRange(topRow);
            }
            else
            {
                // Sweep left on bottom, then left-to-right on top
                List<Transform> leftSlice = bottomRow.Take(index + 1).Reverse().ToList();
                orderedOffices.AddRange(leftSlice);
                orderedOffices.AddRange(topRow);
                orderedOffices.AddRange(bottomRow.Skip(index + 1));
            }
        }

        return orderedOffices.Select(t => new Vector3(t.position.x, t.position.y, 0f)).ToList();
    }

    private void DrawPathTiles(List<Vector3> officePoints)
    {
        if (fullTourWaypoints.Count == 0) return;

        float tileSize = grid.nodeRadius * 2f;
        HashSet<Vector2> officeSet = new HashSet<Vector2>(officePoints.Select(p => new Vector2(Mathf.Round(p.x * 100f) / 100f, Mathf.Round(p.y * 100f) / 100f)));

        for (int i = 0; i < fullTourWaypoints.Count; i++)
        {
            Vector3 pos = fullTourWaypoints[i];
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
                sr.sortingOrder = sortingOrder;
                pathTiles.Add(tile);
            }

            tile.transform.position = pos;
            tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);

            SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
            // Color green if this waypoint sits at an office center, else blue
            if (officeSet.Contains(roundPos))
            {
                renderer.color = officeNodeColor;
                renderer.sortingOrder = sortingOrder + 1;
            }
            else
            {
                renderer.color = pathColor;
                renderer.sortingOrder = sortingOrder;
            }
        }
    }

    public void ResetTour()
    {
        fullTourWaypoints.Clear();
        if (clickMarker != null) clickMarker.SetActive(false);
        for (int i = 0; i < pathTiles.Count; i++)
        {
            pathTiles[i].SetActive(false);
        }
    }

    private void SetMarker(ref GameObject marker, string name, Vector3 position, Color color)
    {
        if (marker == null)
        {
            marker = new GameObject(name);
            marker.transform.parent = transform;
            SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.sortingOrder = sortingOrder + 2;
        }

        float markerSize = (grid.nodeRadius * 2f) * 1.5f;
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(markerSize, markerSize, 1f);
        marker.GetComponent<SpriteRenderer>().color = color;
        marker.SetActive(true);
    }
}
