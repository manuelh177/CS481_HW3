
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PotentialFieldManager : MonoBehaviour
{
    [Header("Entity Settings")]
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int agentCount = 10;

    [Header("Starting Area")]
    [SerializeField] private Vector2 spawnAreaMin = new Vector2(-8f, -4f);
    [SerializeField] private Vector2 spawnAreaMax = new Vector2(-5f, 4f);

    [Header("Collision Settings")]
    [SerializeField] private float agentRadius = 0.2f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int maxSpawnAttempts = 100;

    private readonly List<GameObject> agents = new List<GameObject>();

    [Header("Selection Settings")]
    [SerializeField] private NavigationModeManager navigationModeManager;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color unselectedColor = Color.blue;

    private readonly List<GameObject> selectedAgents = new List<GameObject>();

    [Header("Drag Selection")]
    [SerializeField] private float dragThreshold = 10f;

    private Vector2 dragStartPosition;
    private Vector2 dragCurrentPosition;
    private bool isDragging;
    private bool isMouseDown;

    private void Start()
    {
        SpawnAgents();
    }

    private void Update()
    {
        if (navigationModeManager == null ||
            !navigationModeManager.IsPotentialFieldsMode)
        {
            isDragging = false;
            isMouseDown = false;
            return;
        }

        HandleSelection();
        HandleMovementCommands();
    }


    private void HandleSelection()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Record where the mouse was first pressed.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            dragStartPosition = mousePosition;
            dragCurrentPosition = mousePosition;
            isMouseDown = true;
            isDragging = false;
        }

        // Detect when the user starts dragging.
        if (isMouseDown && Mouse.current.leftButton.isPressed)
        {
            dragCurrentPosition = mousePosition;

            if (Vector2.Distance(dragStartPosition, dragCurrentPosition)
                > dragThreshold)
            {
                isDragging = true;
            }
        }

        // Perform selection when the mouse button is released.
        if (isMouseDown && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            bool shiftHeld = Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed ||
                 Keyboard.current.rightShiftKey.isPressed);

            if (isDragging)
            {
                SelectAgentsInRectangle(shiftHeld);
            }
            else
            {
                SelectAgentAtPosition(mousePosition, shiftHeld);
            }

            isDragging = false;
            isMouseDown = false;
        }
    }


    private void HandleMovementCommands()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        if (!Mouse.current.rightButton.wasPressedThisFrame)
            return;

        if (selectedAgents.Count == 0)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        // Find the mouse position on the 2D movement plane (Z = 0).
        Plane movementPlane = new Plane(Vector3.forward, Vector3.zero);

        if (!movementPlane.Raycast(ray, out float distance))
            return;

        Vector3 targetPosition = ray.GetPoint(distance);
        targetPosition.z = 0f;

        foreach (GameObject agent in selectedAgents)
        {
            if (agent == null)
                continue;

            PotentialFieldAgent movement =
                agent.GetComponent<PotentialFieldAgent>();

            if (movement != null)
            {
                bool shiftHeld = Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed ||
                     Keyboard.current.rightShiftKey.isPressed);

                if (shiftHeld)
                {
                    movement.AddWaypoint(targetPosition);
                }
                else
                {
                    movement.SetTarget(targetPosition);
                }
            }
        }

        Debug.Log(
            "Moving " + selectedAgents.Count +
            " agents toward " + targetPosition
        );
    }


    private void SelectAgentAtPosition(Vector2 mousePosition, bool shiftHeld)
    {
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject clickedAgent = hit.collider.gameObject;

            if (agents.Contains(clickedAgent))
            {
                if (!shiftHeld)
                {
                    ClearSelection();
                    SelectAgent(clickedAgent);
                }
                else
                {
                    if (selectedAgents.Contains(clickedAgent))
                        DeselectAgent(clickedAgent);
                    else
                        SelectAgent(clickedAgent);
                }

                return;
            }
        }

        if (!shiftHeld)
            ClearSelection();
    }


    private void SelectAgentsInRectangle(bool shiftHeld)
    {
        if (!shiftHeld)
            ClearSelection();

        float minX = Mathf.Min(dragStartPosition.x, dragCurrentPosition.x);
        float maxX = Mathf.Max(dragStartPosition.x, dragCurrentPosition.x);

        float minY = Mathf.Min(dragStartPosition.y, dragCurrentPosition.y);
        float maxY = Mathf.Max(dragStartPosition.y, dragCurrentPosition.y);

        foreach (GameObject agent in agents)
        {
            if (agent == null)
                continue;

            Vector3 screenPosition =
                mainCamera.WorldToScreenPoint(agent.transform.position);

            if (screenPosition.z <= 0f)
                continue;

            if (screenPosition.x >= minX &&
                screenPosition.x <= maxX &&
                screenPosition.y >= minY &&
                screenPosition.y <= maxY)
            {
                SelectAgent(agent);
            }
        }

        Debug.Log("Total selected agents: " + selectedAgents.Count);
    }


    private void SelectAgent(GameObject agent)
    {
        if (selectedAgents.Contains(agent))
            return;

        selectedAgents.Add(agent);

        SpriteRenderer renderer = agent.GetComponent<SpriteRenderer>();

        if (renderer != null)
            renderer.color = selectedColor;

        Debug.Log("Selected: " + agent.name);
    }

    private void DeselectAgent(GameObject agent)
    {
        selectedAgents.Remove(agent);

        SpriteRenderer renderer = agent.GetComponent<SpriteRenderer>();

        if (renderer != null)
            renderer.color = unselectedColor;

        Debug.Log("Deselected: " + agent.name);
    }

    private void ClearSelection()
    {
        foreach (GameObject agent in selectedAgents)
        {
            if (agent == null)
                continue;

            SpriteRenderer renderer = agent.GetComponent<SpriteRenderer>();

            if (renderer != null)
                renderer.color = unselectedColor;
        }

        selectedAgents.Clear();
    }

    private void SpawnAgents()
    {
        if (agentPrefab == null)
        {
            Debug.LogError("PFAgent prefab is not assigned!");
            return;
        }

        for (int i = 0; i < agentCount; i++)
        {
            bool foundPosition = false;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector2 randomPosition = new Vector2(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y)
                );

                Vector3 spawnPosition = new Vector3(
                    randomPosition.x,
                    randomPosition.y,
                    0f
                );

                // Check whether the position overlaps an obstacle.
                bool blocked = Physics.CheckSphere(
                    spawnPosition,
                    agentRadius,
                    obstacleLayer
                );

                if (blocked)
                    continue;

                // Check whether another agent is too close.
                bool tooClose = false;

                foreach (GameObject agent in agents)
                {
                    float distance = Vector3.Distance(
                        spawnPosition,
                        agent.transform.position
                    );

                    if (distance < agentRadius * 2f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                // Spawn the agent.
                GameObject newAgent = Instantiate(
                    agentPrefab,
                    spawnPosition,
                    Quaternion.Euler(0f, 0f, Random.Range(0f, 360f))
                );

                newAgent.name = "PFAgent_" + i;
                agents.Add(newAgent);

                foundPosition = true;
                break;
            }

            if (!foundPosition)
            {
                Debug.LogWarning(
                    "Could not find a valid position for agent " + i
                );
            }
        }

        Debug.Log("Spawned " + agents.Count + " agents.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            spawnAreaMax.y - spawnAreaMin.y,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }


    private void OnGUI()
    {
        if (!isDragging ||
            navigationModeManager == null ||
            !navigationModeManager.IsPotentialFieldsMode)
        {
            return;
        }

        // Unity GUI uses a top-left origin.
        // Mouse screen coordinates use a bottom-left origin.
        Vector2 start = new Vector2(
            dragStartPosition.x,
            Screen.height - dragStartPosition.y
        );

        Vector2 end = new Vector2(
            dragCurrentPosition.x,
            Screen.height - dragCurrentPosition.y
        );

        Rect selectionRect = new Rect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Abs(end.x - start.x),
            Mathf.Abs(end.y - start.y)
        );

        // Semi-transparent green selection box.
        Color previousColor = GUI.color;

        GUI.color = new Color(0f, 1f, 0f, 0.25f);
        GUI.DrawTexture(selectionRect, Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

}
