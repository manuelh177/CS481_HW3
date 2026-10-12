
using UnityEngine;
using System.Collections.Generic;

public class PotentialFieldAgent : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float stoppingDistance = 0.15f;
    [SerializeField] private float waypointArrivalDistance = 0.3f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private float obstacleDetectionRadius = 1.5f;
    [SerializeField] private float obstacleRepulsionStrength = 3f;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Agent Avoidance")]
    [SerializeField] private float agentDetectionRadius = 0.8f;
    [SerializeField] private float agentRepulsionStrength = 2f;

    private Vector3 targetPosition;
    private bool hasTarget = false;

    private readonly Queue<Vector3> waypoints = new Queue<Vector3>();

    private void Update()
    {
        if (!hasTarget)
            return;

        MoveTowardTarget();
    }

    public void SetTarget(Vector3 target)
    {
        target.z = 0f;

        waypoints.Clear();
        targetPosition = target;
        hasTarget = true;
    }

    public void AddWaypoint(Vector3 waypoint)
    {
        waypoint.z = 0f;

        if (!hasTarget)
        {
            targetPosition = waypoint;
            hasTarget = true;
        }
        else
        {
            waypoints.Enqueue(waypoint);
        }
    }

    private void MoveTowardTarget()
    {
        Vector3 direction = targetPosition - transform.position;
        direction.z = 0f;

        float distance = direction.magnitude;

        float arrivalDistance = waypoints.Count > 0
            ? waypointArrivalDistance
            : stoppingDistance;

        // Advance through reached waypoints without pausing.
        while (distance <= arrivalDistance)
        {
            if (waypoints.Count > 0)
            {
                targetPosition = waypoints.Dequeue();

                direction = targetPosition - transform.position;
                direction.z = 0f;

                distance = direction.magnitude;

                arrivalDistance = waypoints.Count > 0
                    ? waypointArrivalDistance
                    : stoppingDistance;
            }
            else
            {
                hasTarget = false;
                return;
            }
        }

        // Attractive force toward the current waypoint.
        Vector3 attractiveForce = direction.normalized;

        // Repulsive forces from obstacles and nearby agents.
        Vector3 obstacleRepulsion = CalculateObstacleRepulsion();
        Vector3 agentRepulsion = CalculateAgentRepulsion();

        // Combine the forces to determine movement direction.
        Vector3 combinedForce =
            attractiveForce + obstacleRepulsion + agentRepulsion;

        if (combinedForce.sqrMagnitude < 0.0001f)
            return;

        Vector3 movement =
            combinedForce.normalized * moveSpeed * Time.deltaTime;

        transform.position += movement;
    }

    private Vector3 CalculateObstacleRepulsion()
    {
        Vector3 totalRepulsion = Vector3.zero;

        Collider[] nearbyObstacles = Physics.OverlapSphere(
            transform.position,
            obstacleDetectionRadius,
            obstacleLayer
        );

        foreach (Collider obstacle in nearbyObstacles)
        {
            Vector3 closestPoint =
                obstacle.ClosestPoint(transform.position);

            Vector3 awayDirection =
                transform.position - closestPoint;

            awayDirection.z = 0f;

            float distance = awayDirection.magnitude;

            if (distance < 0.001f)
                continue;

            float strength = 1f - Mathf.Clamp01(
                distance / obstacleDetectionRadius
            );

            totalRepulsion +=
                awayDirection.normalized *
                strength *
                obstacleRepulsionStrength;
        }

        return totalRepulsion;
    }

    private Vector3 CalculateAgentRepulsion()
    {
        Vector3 totalRepulsion = Vector3.zero;

        PotentialFieldAgent[] allAgents =
            FindObjectsByType<PotentialFieldAgent>(
                FindObjectsSortMode.None
            );

        foreach (PotentialFieldAgent otherAgent in allAgents)
        {
            if (otherAgent == this)
                continue;

            Vector3 awayDirection =
                transform.position - otherAgent.transform.position;

            awayDirection.z = 0f;

            float distance = awayDirection.magnitude;

            if (distance < 0.001f || distance > agentDetectionRadius)
                continue;

            float strength =
                1f - (distance / agentDetectionRadius);

            totalRepulsion +=
                awayDirection.normalized *
                strength *
                agentRepulsionStrength;
        }

        return totalRepulsion;
    }
}
