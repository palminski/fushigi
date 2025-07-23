using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfinderController : MonoBehaviour
{
    // public PathfindingGrid grid;

    //Combined From Pathfinding Grid
    public Tilemap walkableTilemap;
    public Tilemap hardTerrainTilemap; // This may end up being an array so that different tilemaps can have different harness values.
    private Dictionary<Vector3Int, Node> nodes;

    // Start is called before the first frame update
    // void Start()
    // {
    //     grid = GetComponent<PathfindingGrid>();
    // }

    void Awake()
    {
        GenerateGrid();
    }

    public List<Node> FindPath(Vector3Int startPosition, Vector3Int targetPosition)
    {
        Node startingNode = GetNode(startPosition);
        Node targetNode = GetNode(targetPosition);

        if (startingNode == null || targetNode == null || !targetNode.walkable)
        {

            return null;
        }

        var openSet = new List<Node> { startingNode };
        var closedSet = new HashSet<Node>();

        foreach (var node in GetAllNodes())
        {
            node.gCost = int.MaxValue;
            node.hCost = 0;
            node.parent = null;
        }

        startingNode.gCost = 0;
        startingNode.hCost = GetDistance(startingNode, targetNode);

        // Begin A* loop
        while (openSet.Count > 0)
        {
            // Question
            Node current = openSet.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();
            if (current == targetNode)
            {
                return RetracePath(startingNode, targetNode);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;
                int tenativeGCost = current.gCost + neighbor.movementCost;

                if (tenativeGCost < neighbor.gCost)
                {
                    neighbor.gCost = tenativeGCost;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        return null;
    }

    public List<Node> GetReachableNodes(Vector3Int startPosition, int range)
    {
        List<Node> reachable = new List<Node>();

        var startingNode = GetNode(startPosition);
        if (startPosition == null) return reachable;

        Dictionary<Node, int> costSoFar = new Dictionary<Node, int>();
        Queue<Node> frontier = new Queue<Node>();

        frontier.Enqueue(startingNode);
        costSoFar[startingNode] = 0;

        while (frontier.Count > 0)
        {
            Node current = frontier.Dequeue();
            reachable.Add(current);

            foreach (Node neighbor in GetNeighbors(current))
            {
                if (!neighbor.walkable) continue;

                int newCost = costSoFar[current] + neighbor.movementCost;
                if (newCost <= range && (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]))
                {
                    costSoFar[neighbor] = newCost;
                    frontier.Enqueue(neighbor);
                }
            }
        }

        return reachable;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node current = endNode;

        while (current != startNode)
        {
            path.Add(current);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridPosition.x - b.gridPosition.x);
        int dy = Mathf.Abs(a.gridPosition.y - b.gridPosition.y);
        return dx + dy;
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        Vector3Int[] directions = {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.up,
            Vector3Int.down,
        };

        foreach (var dir in directions)
        {
            Vector3Int neighborPosition = node.gridPosition + dir;
            Node neighbor = GetNode(neighborPosition);
            if (neighbor != null)
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public float GetPathCost(List<Node> path)
    {
        float cost = 0;
        foreach (Node node in path)
        {
            cost += node.movementCost;
        }
        return cost;
    }

    public List<Node> TrimPathToMovementRange(List<Node> fullPath, int movementRange)
    {
        List<Node> trimmedPath = new List<Node>();
        float currentCost = 0f;

        foreach (Node node in fullPath)
        {
            currentCost += node.movementCost;
            if (currentCost > movementRange)
            {
                break;
            }
            trimmedPath.Add(node);
        }
        return trimmedPath;
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
