using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class EnemyUnit : MapObject
{
    public Tilemap tilemap;

    [HideInInspector] public Mover mover;

    public bool canAct = true;

    private SpriteRenderer spriteRenderer;

    private Color baseColor;
    // Start is called before the first frame update
    void Awake()
    {

        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        mover = GetComponent<Mover>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PerformTurnAction()
    {
        MapManager.Instance.RefreshMap();
        Node targetNode = FindTargetTile();
        if (targetNode != null)
        {
            List<Node> path = PathfinderController.Instance.FindPath(mover.transform.position, targetNode.gridPosition);
            mover.StartMoving(path);

        }
    }

    private Node FindTargetTile()
    {
        List<Node> reachableNodes = PathfinderController.Instance.GetReachableNodes(mover.transform.position, 8); //Starting Range
        var playerUnits = FindObjectsOfType<PlayerUnit>();
        List<Node> shortestPath = null;
        Node targetNode = null;
        foreach (Node reachableNode in reachableNodes)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(reachableNode.gridPosition);
            bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || obj is EnemyUnit);
            if (!validTile) continue;
            foreach (PlayerUnit playerUnit in playerUnits)
            {

                Vector3 targetGridPoition = playerUnit.transform.position;
                Vector3 thisGridPosition = reachableNode.gridPosition;

                List<Node> path = PathfinderController.Instance.FindPath(thisGridPosition, targetGridPoition, true);

                if (shortestPath == null || shortestPath.Count > path.Count)
                {
                    shortestPath = path;
                    targetNode = reachableNode;
                }
            }
        }
        return targetNode;
    }

    // public void SetInactive()
    // {
    //     canAct = false;
    //     float lumunence = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
    //     Color inactiveColor = new Color(lumunence, lumunence, lumunence, 0.5f);
    //     spriteRenderer.color = inactiveColor;
    //     GameController.Instance.CheckAndChangePhase(); //Temporary for now
    // }

    // public void SetActive()
    // {
    //     canAct = true;
    //     spriteRenderer.color = baseColor;

    // }
}
