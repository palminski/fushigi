using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public PathfindingGrid grid;

    // Start is called before the first frame update
    void Start()
    {
        grid = GetComponent<PathfindingGrid>();
    }

    public List<Node> FindPath(Vector3Int startPosition, Vector3Int targetPosition)
    {
        Node startingNode = grid.GetNode(startPosition);
        Node targetNode = grid.GetNode(targetPosition);

        if (startingNode == null || targetNode == null || !targetNode.walkable)
        {
            return null;
        }

        var openSet = new List<Node> { startingNode };
        var closedSet = new HashSet<Node>();

        foreach (var node in grid.GetAllNodes())
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

        var startingNode = grid.GetNode(startPosition);
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
            Node neighbor = grid.GetNode(neighborPosition);
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

}
