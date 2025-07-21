using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Vector3Int gridPosition;
    public bool walkable;
    public int movementCost;

    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    public Node parent;

    public Node(Vector3Int gridPosition, bool walkable, int movementCost = 1) {
        this.gridPosition = gridPosition;
        this.walkable = walkable;
        this.movementCost = movementCost;
    }

    public static implicit operator Vector3(Node v)
    {
        throw new NotImplementedException();
    }
}
