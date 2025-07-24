using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class InputController : MonoBehaviour
{
    public Tilemap tilemap;
    public Tilemap overlayTilemap;
    public Tile greenOverlay;
    public Tile redOverlay;
    public Mover currentMover;

    public int moveStat = 8;
    public Transform reticalTransform;
    public LineRenderer lineRenderer;
    private GameInput inputActions;
    // Start is called before the first frame update
    void Awake()
    {
        inputActions = new GameInput();
    }

    void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Click.performed += OnClick;
    }

    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
        inputActions.Gameplay.Disable();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

        if (GameController.Instance.gamePhase == GamePhase.Enemy) return;

        if (currentMover)
        {
            Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);
            TileBase tile = tilemap.GetTile(gridPosition);

            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));

            Vector3Int playerGrid = tilemap.WorldToCell(currentMover.transform.position);
            Vector3Int targetGrid = tilemap.WorldToCell(worldPosition);

            if (playerGrid == targetGrid)
            {
                currentMover.DeactivatePlayer();
                currentMover = null;
                overlayTilemap.ClearAllTiles();
                ClearLine();
            }

            bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || obj is EnemyUnit);
            if (validTile && tile != null)
            {

                List<Node> path = PathfinderController.Instance.FindPath(playerGrid, targetGrid);
                if (path != null && path.Count > 0 && path.Count <= moveStat)
                {
                    overlayTilemap.ClearAllTiles();
                    currentMover.StartMoving(path, moveStat);
                    currentMover = null;
                    ClearLine();
                }
            }
        }
        else
        {
            MapManager.Instance.RefreshMap();
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            PlayerUnit playerUnit = objectsAtTile.OfType<PlayerUnit>().FirstOrDefault();

            if (playerUnit != null && playerUnit.canAct)
            {
                Mover mover = playerUnit.mover;
                if (mover != null)
                {
                    currentMover = mover;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, reticalTransform.position.z));

        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);
        TileBase tile = tilemap.GetTile(gridPosition); // This is the currently highlighted tile

        if (currentMover)
        {
            Vector3Int playerGrid = tilemap.WorldToCell(currentMover.transform.position);
            List<Node> path = PathfinderController.Instance.FindPath(playerGrid, gridPosition); //Path to the tile 

            if (!currentMover.isMoving)
            {
                ShowMovementRange();
            }

            if (tile != null && !currentMover.isMoving && path != null && PathfinderController.Instance.GetPathCost(path) <= moveStat)
            {
                if (reticalTransform.gameObject.activeSelf == false) DrawPath(gridPosition);
                reticalTransform.gameObject.SetActive(true);
            }
            else
            {
                reticalTransform.gameObject.SetActive(false);
                ClearLine();
            }

            //DrawLine if Retical is in diff position
            if (reticalTransform.position != tilemap.GetCellCenterWorld(gridPosition))
            {
                if (reticalTransform.gameObject.activeSelf == true) DrawPath(gridPosition);
                reticalTransform.position = tilemap.GetCellCenterWorld(gridPosition);
            }
        }
        else
        {
            reticalTransform.position = tilemap.GetCellCenterWorld(gridPosition);
        }
    }

    public void DrawPath(Vector3Int gridPosition)
    {

        Vector3Int playerGrid = tilemap.WorldToCell(currentMover.transform.position);
        Vector3Int targetGrid = gridPosition;



        List<Node> path = PathfinderController.Instance.FindPath(playerGrid, targetGrid);


        if (path == null || path.Count == 0 || PathfinderController.Instance.GetPathCost(path) > moveStat)
        {
            ClearLine();
            return;
        }

        lineRenderer.positionCount = path.Count + 1;
        lineRenderer.SetPosition(0, tilemap.GetCellCenterWorld(path[0].parent.gridPosition));
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i + 1, tilemap.GetCellCenterWorld(path[i].gridPosition));
        }
    }

    public void ShowMovementRange()
    {
        overlayTilemap.ClearAllTiles();

        Vector3Int playerPosition = tilemap.WorldToCell(currentMover.transform.position);
        List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerPosition, moveStat);

        foreach (Node node in PathfinderController.Instance.GetAllNodes())
        {
            if (!node.walkable) continue;
            if (reachable.Contains(node))
            {
                overlayTilemap.SetTile(node.gridPosition, greenOverlay);
                foreach (Node neighbor in PathfinderController.Instance.GetNeighbors(node))
                {
                    if (!reachable.Contains(neighbor))
                    {
                        overlayTilemap.SetTile(neighbor.gridPosition, redOverlay);
                    }
                }
            }

        }
    }

    public void ClearLine()
    {
        lineRenderer.positionCount = 0;
    }
}
