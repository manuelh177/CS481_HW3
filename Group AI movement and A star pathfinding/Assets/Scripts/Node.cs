using UnityEngine;
using System.Collections;

public class Node
{
    public bool walkable;
    public Vector2 worldPos;

    //for A*
    public int gCost;
    public int hCost;

    public Node(bool _walkable, Vector2 _worldPos)
    {
        walkable = _walkable;
        worldPos = _worldPos;
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}
