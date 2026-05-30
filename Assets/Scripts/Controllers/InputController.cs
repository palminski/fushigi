using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public Tilemap tilemap;
    public Tilemap overlayTilemap;
    public Tile greenOverlay;
    public Tile redOverlay;
    public Mover currentMover;

    public Transform reticalTransform;
    public LineRenderer lineRenderer;
    private GameInput inputActions;

    private bool isSelectingAttack = false;
    
    private PlayerUnit pendingAttackUnit;
    private List<EnemyUnit> attackableEnemies = new List<EnemyUnit>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        inputActions = new GameInput();
    }

    void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Click.performed += OnClick;
        inputActions.Gameplay.RightClick.performed += OnRightClick;
    }

    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
        inputActions.Gameplay.RightClick.performed -= OnRightClick;

        inputActions.Gameplay.Disable();
    }

    public void StartAttackTargetSelection(PlayerUnit unit, List<EnemyUnit> enemies)
    {
        pendingAttackUnit = unit;
        attackableEnemies = enemies;
        isSelectingAttack = true;

        overlayTilemap.ClearAllTiles();
        foreach (EnemyUnit enemy in enemies)
        {
            overlayTilemap.SetTile(enemy.GridPosition, redOverlay);
        }
    }

    public void CancelAttackSelection()
    {
        isSelectingAttack = false;
        pendingAttackUnit = null;
        attackableEnemies.Clear();
        overlayTilemap.ClearAllTiles();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

        if (GameController.Instance.gamePhase == GamePhase.Enemy) return;
        

        //Player is selecting an enemy to attack
        if (isSelectingAttack)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
            if (clickedEnemy != null && attackableEnemies.Contains(clickedEnemy))
            {
                PlayerUnit attacker = pendingAttackUnit;
                CancelAttackSelection();

                int dist = Mathf.Abs(attacker.GridPosition.x - clickedEnemy.GridPosition.x) + Mathf.Abs(attacker.GridPosition.y - clickedEnemy.GridPosition.y);
                
                Weapon equipped = attacker.inventory.EquippedWeapon;
                Weapon weapon = (equipped != null && equipped.CanHitAt(dist)) ? equipped : FindBestAttackOption(attacker, clickedEnemy, new List<Node> {PathfinderController.Instance.GetNode(attacker.transform.position)}).weapon;
                if (weapon != null) attacker.Attack(clickedEnemy, weapon);

                attacker.SetInactive();
            }
            return;
        }

        if (ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition))    ;
            //If Unit clicked after moving act as if wait was clicked
            if (objectsAtTile.Contains(ActionMenuController.Instance.PendingUnit)) ActionMenuController.Instance.OnWaitClicked();
            return;
        }
        

        //Unit Is Selected, Awaiting User to pick where to go to
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
                return;
            }
            
            EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
            if (clickedEnemy != null && !currentMover.isMoving)
            {
                MovementClass mc = currentMover.playerUnit.unitAttributes.movementClass;
                List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerGrid, currentMover.playerUnit.unitAttributes.movement, mc);
                var (weapon, bestTile) = FindBestAttackOption(currentMover.playerUnit, clickedEnemy, reachable);
                if (weapon != null && bestTile != null)
                {
                    List<Node>path = PathfinderController.Instance.FindPath(playerGrid, bestTile.gridPosition, mc);
                    currentMover.QueueAttack(clickedEnemy, weapon);
                    overlayTilemap.ClearAllTiles();
                    currentMover.StartMoving(path, currentMover.playerUnit.unitAttributes.movement);
                    currentMover = null;
                    ClearLine();
                }
                return;
            }

            bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || obj is EnemyUnit);
            if (validTile && tile != null)
            {

                MovementClass movementClass = currentMover.playerUnit.unitAttributes.movementClass;
                List<Node> path = PathfinderController.Instance.FindPath(playerGrid, targetGrid, movementClass);
                if (path != null && path.Count > 0 && PathfinderController.Instance.GetPathCost(path, movementClass) <= currentMover.playerUnit.unitAttributes.movement)
                {
                    overlayTilemap.ClearAllTiles();
                    currentMover.StartMoving(path, currentMover.playerUnit.unitAttributes.movement);
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
                if (mover != null && !mover.isMoving)
                {
                    currentMover = mover;
                }
            }
        }
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if(ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        {
            ActionMenuController.Instance.CancelMenu();
        }
        else if (isSelectingAttack)
        {
            PlayerUnit unit = pendingAttackUnit;
            CancelAttackSelection();
            unit.mover.CancelMove();
            currentMover = unit.mover;
        }
        else if (currentMover != null)
        {
            currentMover = null;
            overlayTilemap.ClearAllTiles();
            ClearLine();
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
            MovementClass movementClass = currentMover.playerUnit.unitAttributes.movementClass;
            List<Node> path = PathfinderController.Instance.FindPath(playerGrid, gridPosition, movementClass);

            if (!currentMover.isMoving)
            {
                ShowMovementRange();
            }

            List<MapObject> hoveredObjects = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            EnemyUnit hoveredEnemy = hoveredObjects.OfType<EnemyUnit>().FirstOrDefault();

            if (hoveredEnemy != null && !currentMover.isMoving)
            {
                List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerGrid, currentMover.playerUnit.unitAttributes.movement, movementClass);
                var (_, bestTile) = FindBestAttackOption(currentMover.playerUnit, hoveredEnemy, reachable);
                
                if (bestTile != null)
                    DrawPath(bestTile.gridPosition);
                else
                    ClearLine();
                // reticalTransform.gameObject.SetActive(false);
            }
            else if (tile != null && !currentMover.isMoving && path != null && PathfinderController.Instance.GetPathCost(path, movementClass) <= currentMover.playerUnit.unitAttributes.movement)
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



        MovementClass movementClass = currentMover.playerUnit.unitAttributes.movementClass;
        List<Node> path = PathfinderController.Instance.FindPath(playerGrid, targetGrid, movementClass);

        if (path == null || path.Count == 0 || PathfinderController.Instance.GetPathCost(path, movementClass) > currentMover.playerUnit.unitAttributes.movement)
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
        MovementClass movementClass = currentMover.playerUnit.unitAttributes.movementClass;
        List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerPosition, currentMover.playerUnit.unitAttributes.movement, movementClass);

        var reachableSet = new HashSet<Node>(reachable);
        var attackableSet = new HashSet<Node>();

        foreach (Node node in reachable)
        {
            foreach (Item item in currentMover.playerUnit.inventory.items)
            {
                if (item is not Weapon weapon) continue;
                List<Node> weaponRange = PathfinderController.Instance.GetAttackableTiles(
                    tilemap.GetCellCenterWorld(node.gridPosition),
                    weapon.minRange,
                    weapon.maxRange
                );
                foreach (Node attackNode in weaponRange)
                {
                    attackableSet.Add(attackNode);
                }
            }
        }


        foreach (Node node in PathfinderController.Instance.GetAllNodes())
        {
            if (reachableSet.Contains(node))
            {
                overlayTilemap.SetTile(node.gridPosition, greenOverlay);
            }
            else if (attackableSet.Contains(node))
            {
                overlayTilemap.SetTile(node.gridPosition, redOverlay);
            }

        }
    }

    public void ClearLine()
    {
        lineRenderer.positionCount = 0;
    }

    private (Weapon weapon, Node tile) FindBestAttackOption(PlayerUnit unit, EnemyUnit target, List<Node> reachable)
    {
        int bestScore = int.MinValue;
        Weapon bestWeapon = null;
        Node bestTile = null;
        Vector3Int targetPosition = target.GridPosition;

        foreach(Node node in reachable)
        {
            var occupants = MapManager.Instance.GetObjectsAt(node.gridPosition);
            if (occupants.Any(o => o is PlayerUnit p && p != unit || o is EnemyUnit)) continue;

            foreach (Item item in unit.inventory.items)
            {
                if (item is not Weapon weapon) continue;
                int dist = Mathf.Abs(node.gridPosition.x - targetPosition.x) + Mathf.Abs(node.gridPosition.y - targetPosition.y);
                if (dist < weapon.minRange || dist > weapon.maxRange) continue;

                CombatPreview preview = CombatCalculator.Preview(unit, target, weapon, node.gridPosition);
                int score = preview.damageDealt;
                if (preview.killsDefender) score += 100;
                if (!preview.defenderCanCounter) score += 1000;

                if (score > bestScore) {bestScore = score; bestWeapon = weapon; bestTile = node;}
            }
        }

        return (bestWeapon, bestTile);
    }
}
