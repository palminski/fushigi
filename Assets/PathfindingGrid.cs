using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public class PathfindingGrid : MonoBehaviour
{
    public Tilemap walkableTilemap;
    public Tilemap hardTerrainTilemap; // This may end up being an array so that different tilemaps can have different harness values.

    

    private Dictionary<Vector3Int, Node> nodes;
    void Awake()
    {
        GenerateGrid();
        
    }

    void GenerateGrid()
    {
        walkableTilemap.CompressBounds();
        BoundsInt bounds = walkableTilemap.cellBounds;

        int width = bounds.size.x;
        int height = bounds.size.y;

        Vector3Int origin = bounds.min;

        nodes = new Dictionary<Vector3Int, Node>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int position = new Vector3Int(origin.x + x, origin.y + y, 0);

                bool walkable = walkableTilemap.HasTile(position);
                if (walkable)
                {
                    int movementCost = hardTerrainTilemap.HasTile(position) ? 2 : 1;
                    Node newNode = new Node(position, true, movementCost);
                    nodes[position] = newNode;
                }
                else
                {
                    Node newNode = new Node(position, false);
                    nodes[position] = newNode;
                }
            }
        }
    }

    public Node GetNode(Vector3Int position)
    {
        return nodes.TryGetValue(position, out var node) ? node : null;
    }

    public IEnumerable<Node> GetAllNodes() => nodes.Values;

}
