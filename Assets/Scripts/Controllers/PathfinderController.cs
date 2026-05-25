using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfinderController : MonoBehaviour
{
    // Pathfinder controller is a singleton object that handles the nodes representing tiles in the game. While its primary function is finding paths from one position to another, it
    // also handles functions related to getting information about the grid and nodes, such as returning a node from a position, or calculating the range that a unit can move.
    public static PathfinderController Instance { get; private set; }

    [System.Serializable]
    public struct TerrainLayer
    {
        public Tilemap tilemap;
        public TerrainType terrainType;
    }

    public Tilemap walkableTilemap;
    public List<TerrainLayer> terrainLayers;
    private Dictionary<Vector3Int, Node> nodes;


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        GenerateGrid();
    }

    // =======================================================================================================================================================================================

    // Public Methods

    // =======================================================================================================================================================================================

    // Takes a starting and ending location and finds the shortest path between the two
    public List<Node> FindPath(Vector3 startPosition, Vector3 targetPosition, MovementClass movementClass, bool allowIllegalTarget = false)
    {
        GenerateGrid();
        Vector3Int startGridPosition = walkableTilemap.WorldToCell(startPosition);
        Vector3Int targetGridPosition = walkableTilemap.WorldToCell(targetPosition);

        Node startingNode = GetNode(startGridPosition);
        Node targetNode = GetNode(targetGridPosition);

        if (startingNode == null || targetNode == null || (!allowIllegalTarget && !targetNode.walkable))
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

        while (openSet.Count > 0)
        {
            Node current = openSet.OrderBy(n => n.fCost).ThenBy(n => n.hCost).First();
            if (current == targetNode)
            {
                return RetracePath(startingNode, targetNode);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Node neighbor in GetNeighbors(current))
            {
                bool blocked = neighbor != targetNode && (!neighbor.walkable || neighbor.IsImpassableFor(movementClass));
                if (blocked || closedSet.Contains(neighbor)) continue;

                int tentativeGCost = current.gCost + neighbor.GetMovementCost(movementClass);

                if (tentativeGCost < neighbor.gCost)
                {
                    neighbor.gCost = tentativeGCost;
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

    // Gets reachable nodes from a start position in a certain movement range taking into account each node's movement cost
    public List<Node> GetReachableNodes(Vector3 startPosition, int range, MovementClass movementClass)
    {
        GenerateGrid();
        Vector3Int startGridPosition = walkableTilemap.WorldToCell(startPosition);
        List<Node> reachable = new List<Node>();

        var startingNode = GetNode(startGridPosition);
        if (startingNode == null) return reachable;

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
                if (!neighbor.walkable || neighbor.IsImpassableFor(movementClass)) continue;

                int newCost = costSoFar[current] + neighbor.GetMovementCost(movementClass);
                if (newCost <= range && (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]))
                {
                    costSoFar[neighbor] = newCost;
                    frontier.Enqueue(neighbor);
                }
            }
        }
        return reachable;
    }

    // Takes a path and an allowed movement range and returns the same path trimmed to not exceed said movement range
    public List<Node> TrimPathToMovementRange(List<Node> fullPath, int movementRange, MovementClass movementClass)
    {
        List<Node> trimmedPath = new List<Node>();
        float currentCost = 0f;

        foreach (Node node in fullPath)
        {
            currentCost += node.GetMovementCost(movementClass);
            if (currentCost > movementRange) break;
            trimmedPath.Add(node);
        }
        return trimmedPath;
    }

    public List<Node> GetAttackableTiles(Vector3 startPosition, int minRange = 1, int maxRange = 1)
    {
        Vector3Int startGridPosition = walkableTilemap.WorldToCell(startPosition);
        if (GetNode(startGridPosition) == null) return new List<Node>();

        List<Node> attackable = new List<Node>();

        for (int dx = -maxRange; dx <= maxRange; dx++)
        {
            for (int dy = -maxRange; dy <= maxRange; dy++)
            {
                int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist < minRange || dist > maxRange) continue;

                Node neighbor = GetNode(startGridPosition + new Vector3Int(dx, dy, 0));
                if (neighbor != null)
                    attackable.Add(neighbor);
            }
        }
        return attackable;
    }

    // Returns all nodes neighboring a given node
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

    // Returns the movement cost of a path for a given movement class
    public float GetPathCost(List<Node> path, MovementClass movementClass)
    {
        float cost = 0;
        foreach (Node node in path)
        {
            cost += node.GetMovementCost(movementClass);
        }
        return cost;
    }

    // Given a position returns the node associated with that position
    public Node GetNode(Vector3 position)
    {
        Vector3Int convertedPosition = walkableTilemap.WorldToCell(position);
        return nodes.TryGetValue(convertedPosition, out var node) ? node : null;
    }


    // =======================================================================================================================================================================================

    // Internal Methods

    // =======================================================================================================================================================================================

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

    public void GenerateGrid()
    {
        MapManager.Instance.RefreshMap();
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

                var objectsAtTile = MapManager.Instance.GetObjectsAt(position);

                bool walkable = walkableTilemap.HasTile(position)
                    && !(GameController.Instance.gamePhase == GamePhase.Player && objectsAtTile.Any(obj => obj is EnemyUnit))
                    && !(GameController.Instance.gamePhase == GamePhase.Enemy && objectsAtTile.Any(obj => obj is PlayerUnit));

                TerrainType terrainType = null;
                foreach (var layer in terrainLayers)
                {
                    if (layer.tilemap != null && layer.tilemap.HasTile(position))
                    {
                        terrainType = layer.terrainType;
                        break;
                    }
                }

                nodes[position] = new Node(position, walkable, terrainType);
            }
        }
    }

    public IEnumerable<Node> GetAllNodes() => nodes.Values;
}
