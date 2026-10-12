using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pathfinding : MonoBehaviour
{
    public Grid grid;
    public Transform seeker, target;

    private void Update()
    {
        if(seeker != null && target != null)
            FindPath(seeker.position, target.position);
    }

    void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                RetracePath(startNode, targetNode);
                return;
            }

            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }
    }

    void RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Add(currentNode);
        path.Reverse();

        grid.path = path;
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if (dstX > dstY)
        {
            return 14 * dstY + 10 * (dstX - dstY);
        }
        return 14 * dstX + 10 * (dstY - dstX);
    }
    
    public Vector3[] FindPathWaypoints(Vector3 startPos, Vector3 targetPos)
{
    Node startNode = grid.NodeFromWorldPoint(startPos);
    Node targetNode = grid.NodeFromWorldPoint(targetPos);

    // Clear previous A* search information.
    grid.ResetPathfindingNodes();

    // The starting node has no travel cost.
    startNode.gCost = 0;

    if (!startNode.walkable || !targetNode.walkable)
    {
        return null;
    }

    List<Node> openSet = new List<Node>();
    HashSet<Node> closedSet = new HashSet<Node>();
    openSet.Add(startNode);

    while (openSet.Count > 0)
    {
        Node currentNode = openSet[0];
        for (int i = 1; i < openSet.Count; i++)
        {
            if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
            {
                currentNode = openSet[i];
            }
        }

        openSet.Remove(currentNode);
        closedSet.Add(currentNode);

        if (currentNode == targetNode)
        {
            return RetracePathWaypoints(startNode, targetNode);
        }

        foreach (Node neighbor in grid.GetNeighbors(currentNode))
        {
            if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

            int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
            if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
            {
                neighbor.gCost = newMovementCostToNeighbor;
                neighbor.hCost = GetDistance(neighbor, targetNode);
                neighbor.parent = currentNode;

                if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
            }
        }
    }

    return null; // No path found
}

private Vector3[] RetracePathWaypoints(Node startNode, Node endNode)
{
    List<Vector3> waypoints = new List<Vector3>();
    Node currentNode = endNode;

    while (currentNode != startNode)
    {
        waypoints.Add(new Vector3(currentNode.worldPos.x, currentNode.worldPos.y, 0f));
        currentNode = currentNode.parent;
    }
    waypoints.Add(new Vector3(startNode.worldPos.x, startNode.worldPos.y, 0f));
    waypoints.Reverse();

    return waypoints.ToArray();
}
    
}
