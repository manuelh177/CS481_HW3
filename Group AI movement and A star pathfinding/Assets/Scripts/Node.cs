using UnityEngine;
using System.Collections;

public class Node
{
    public bool walkable;
    public Vector2 worldPos;
    public int gridX;
    public int gridY;

    //for A*
    public int gCost;
    public int hCost;
    public Node parent;

    public Node(bool _walkable, Vector2 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPos = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}
