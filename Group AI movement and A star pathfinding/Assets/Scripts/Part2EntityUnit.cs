using System.Collections;
using UnityEngine;

public class Part2EntityUnit : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Color normalColor = Color.cyan;
    private Color selectedColor = Color.magenta;
    private bool isSelected = false;
    private Coroutine followRoutine;

    public void Initialize(Color defaultColor, Color highlightColor)
    {
        normalColor = defaultColor;
        selectedColor = highlightColor;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void FollowPath(Vector3[] waypoints)
    {
        if (followRoutine != null)
        {
            StopCoroutine(followRoutine);
        }
        followRoutine = StartCoroutine(FollowPathRoutine(waypoints));
    }

    private IEnumerator FollowPathRoutine(Vector3[] waypoints)
    {
        if (waypoints == null || waypoints.Length == 0) yield break;

        int targetIndex = 0;
        Vector3 currentWaypoint = waypoints[0];

        while (true)
        {
            // Move toward waypoint
            transform.position = Vector3.MoveTowards(transform.position, currentWaypoint, moveSpeed * Time.deltaTime);

            // Rotate toward direction of movement
            Vector3 moveDirection = (currentWaypoint - transform.position).normalized;
            if (moveDirection != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                Quaternion targetRot = Quaternion.Euler(0f, 0f, angle - 90f); // Adjust for upward-pointing sprites
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Check if reached waypoint
            if (Vector3.Distance(transform.position, currentWaypoint) < 0.05f)
            {
                targetIndex++;
                if (targetIndex >= waypoints.Length)
                {
                    yield break; // Reached target location
                }
                currentWaypoint = waypoints[targetIndex];
            }

            yield return null;
        }
    }
}
