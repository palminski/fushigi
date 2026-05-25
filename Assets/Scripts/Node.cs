using System;
using UnityEngine;

public class Node
{
    public Vector3Int gridPosition;
    public bool walkable;
    public TerrainType terrainType;

    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    public Node parent;

    public Node(Vector3Int gridPosition, bool walkable, TerrainType terrainType = null)
    {
        this.gridPosition = gridPosition;
        this.walkable = walkable;
        this.terrainType = terrainType;
    }

    public int GetMovementCost(MovementClass movementClass)
    {
        return terrainType != null ? terrainType.GetCostFor(movementClass) : 1;
    }

    public bool IsImpassableFor(MovementClass movementClass)
    {
        return terrainType != null && terrainType.IsImpassableFor(movementClass);
    }

    public static implicit operator Vector3(Node v)
    {
        throw new NotImplementedException();
    }
}
