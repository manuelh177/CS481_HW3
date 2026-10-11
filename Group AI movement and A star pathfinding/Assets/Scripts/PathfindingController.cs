using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathfindingController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Pathfinding pathfinding;
    [SerializeField] private Grid grid;
    [SerializeField] private Camera mainCamera;

    [Header("Visual Display")]
    [SerializeField] private Sprite squareSprite; // Assign Unity's default Square sprite
    [SerializeField] private Color startColor = Color.orange;
    [SerializeField] private Color targetColor = Color.red;
    [SerializeField] private Color pathColor = Color.blue;
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private float endpointScaleMultiplier = 2f; // Makes start & target squares 2x larger

    // Internal dummy transforms passed to Pathfinding.seeker and Pathfinding.target
    private Transform seekerPoint;
    private Transform targetPoint;

    // Visual tiles
    private readonly List<GameObject> pathTiles = new List<GameObject>();
    private GameObject startTile;
    private GameObject targetTile;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        GameObject seekerObj = new GameObject("SeekerPoint");
        seekerObj.transform.parent = transform;
        seekerPoint = seekerObj.transform;

        GameObject targetObj = new GameObject("TargetPoint");
        targetObj.transform.parent = transform;
        targetPoint = targetObj.transform;
    }

    private void Update()
    {
        HandleResetInput();
        HandleSelectionInput();
        UpdatePathVisuals();
    }

    private void HandleResetInput()
    {
        // When 'C' is pressed, reset pathfinding targets and clear all visual squares
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            ResetPath();
        }
    }

    public void ResetPath()
    {
        // Clear references so Pathfinding stops computing the old path
        if (pathfinding != null)
        {
            pathfinding.seeker = null;
            pathfinding.target = null;
        }

        if (grid != null && grid.path != null)
        {
            grid.path.Clear();
        }

        // Hide markers
        if (startTile != null) startTile.SetActive(false);
        if (targetTile != null) targetTile.SetActive(false);

        // Hide all path tiles
        ClearPathTiles();
    }

    private void HandleSelectionInput()
    {
        if (Mouse.current == null || grid == null || pathfinding == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, -mainCamera.transform.position.z));
        worldPos.z = 0f;

        // Left-click: Set Start Point
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Node node = grid.NodeFromWorldPoint(worldPos);
            if (node != null && node.walkable)
            {
                seekerPoint.position = new Vector3(node.worldPos.x, node.worldPos.y, 0f);
                pathfinding.seeker = seekerPoint;

                SetMarker(ref startTile, "StartMarker", seekerPoint.position, startColor);
            }
        }

        // Right-click: Set Target Point
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Node node = grid.NodeFromWorldPoint(worldPos);
            if (node != null && node.walkable)
            {
                targetPoint.position = new Vector3(node.worldPos.x, node.worldPos.y, 0f);
                pathfinding.target = targetPoint;

                SetMarker(ref targetTile, "TargetMarker", targetPoint.position, targetColor);
            }
        }
    }

    private void UpdatePathVisuals()
    {
        // If either endpoint is missing or no path exists, do not display path tiles
        if (pathfinding.seeker == null || pathfinding.target == null || grid.path == null || grid.path.Count == 0)
        {
            ClearPathTiles();
            return;
        }

        float tileSize = grid.nodeRadius * 2f;

        // Ensure enough tiles in the pool
        while (pathTiles.Count < grid.path.Count)
        {
            GameObject tile = new GameObject($"PathTile_{pathTiles.Count}");
            tile.transform.parent = transform;
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = squareSprite;
            sr.sortingOrder = sortingOrder;
            pathTiles.Add(tile);
        }

        // Draw path nodes
        for (int i = 0; i < grid.path.Count; i++)
        {
            Node node = grid.path[i];
            GameObject tile = pathTiles[i];
            tile.SetActive(true);
            tile.transform.position = new Vector3(node.worldPos.x, node.worldPos.y, 0f);

            SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

            // First node is start, last is target, remaining middle steps are blue
            if (i == 0)
            {
                float markerSize = tileSize * endpointScaleMultiplier;
                tile.transform.localScale = new Vector3(markerSize, markerSize, 1f);
                sr.color = startColor;
                sr.sortingOrder = sortingOrder + 1; // Render above blue path segments
            }
            else if (i == grid.path.Count - 1)
            {
                float markerSize = tileSize * endpointScaleMultiplier;
                tile.transform.localScale = new Vector3(markerSize, markerSize, 1f);
                sr.color = targetColor;
                sr.sortingOrder = sortingOrder + 1;
            }
            else
            {
                tile.transform.localScale = new Vector3(tileSize, tileSize, 1f);
                sr.color = pathColor;
                sr.sortingOrder = sortingOrder;
            }
        }

        // Deactivate unused tiles
        for (int i = grid.path.Count; i < pathTiles.Count; i++)
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

        float markerSize = (grid.nodeRadius * 2f) * endpointScaleMultiplier;
        marker.transform.position = position;
        marker.transform.localScale = new Vector3(markerSize, markerSize, 1f);
        marker.GetComponent<SpriteRenderer>().color = color;
        marker.SetActive(true);
    }

    private void ClearPathTiles()
    {
        for (int i = 0; i < pathTiles.Count; i++)
        {
            pathTiles[i].SetActive(false);
        }
    }
}
