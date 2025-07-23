using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyUnit : MapObject
{
    public Tilemap tilemap;
    public PathfinderController pathfinder;

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
        var playerUnits = FindObjectsOfType<PlayerUnit>();

        List<Node> shortestPath = null;

        foreach (PlayerUnit playerUnit in playerUnits)
        {
            Vector2 screenPosition = playerUnit.transform.position;
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            Vector3Int targetGrid = tilemap.WorldToCell(screenPosition);

            // Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);
            // TileBase tile = tilemap.GetTile(gridPosition);
            Vector3Int thisGrid = tilemap.WorldToCell(mover.transform.position);

            List<Node> path = pathfinder.FindPath(thisGrid, targetGrid);
            if (shortestPath == null || shortestPath.Count > path.Count)
            {
                shortestPath = path;
            }
        }
        if (shortestPath != null)
        {
            mover.StartMoving(pathfinder.TrimPathToMovementRange(shortestPath, 8));
        }
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
