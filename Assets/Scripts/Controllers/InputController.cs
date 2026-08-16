using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
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

    private bool isSelectingTradePartner = false;
    private PlayerUnit pendingTradeUnit;
    private List<PlayerUnit> tradablePartners = new List<PlayerUnit>();

    private bool isSelectingDropTile = false;
    private PlayerUnit pendingDropUnit;
    private List<Vector3Int> validDropTiles = new List<Vector3Int>();

    private bool isSelectingRescue = false;
    private PlayerUnit pendingRescueUnit;
    private List<PlayerUnit> rescuableAllies = new List<PlayerUnit>();

    private bool isSelectingCapture = false;
    private PlayerUnit pendingCaptureUnit;
    private List<EnemyUnit> capturableEnemies = new List<EnemyUnit>();

    private int weaponCycleIndex = 0;
    private EnemyUnit lastForecastTarget = null;
    private WeaponInstance forecastWeapon = null;
    private Node forecastBestTile = null;

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
        inputActions.Gameplay.Scroll.performed += OnScroll;
    }

    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
        inputActions.Gameplay.RightClick.performed -= OnRightClick;
        inputActions.Gameplay.Scroll.performed -= OnScroll;
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

    public void StartTradePartnerSelection(PlayerUnit unit, List<PlayerUnit> partners)
    {
        pendingTradeUnit = unit;
        tradablePartners = new List<PlayerUnit>(partners);
        isSelectingTradePartner = true;

        overlayTilemap.ClearAllTiles();
        foreach (PlayerUnit partner in partners)
            overlayTilemap.SetTile(partner.GridPosition, greenOverlay);
    }
    public void CancelTradeSelection()
    {
        isSelectingTradePartner = false;
        pendingTradeUnit = null;
        tradablePartners.Clear();
        overlayTilemap.ClearAllTiles();
    }

    public void StartRescueTargetSelection(PlayerUnit unit, List<PlayerUnit> allies)
    {
        pendingRescueUnit = unit;
        rescuableAllies = new List<PlayerUnit>(allies);
        isSelectingRescue = true;

        overlayTilemap.ClearAllTiles();
        foreach (PlayerUnit ally in rescuableAllies)
            overlayTilemap.SetTile(ally.GridPosition, greenOverlay);
    }
    public void CancelRescueSelection()
    {
        isSelectingRescue = false;
        pendingRescueUnit = null;
        rescuableAllies.Clear();
        overlayTilemap.ClearAllTiles();
    }

    public void StartCaptureTargetSelection(PlayerUnit unit, List<EnemyUnit> enemies)
    {
        pendingCaptureUnit = unit;
        capturableEnemies = new List<EnemyUnit>(enemies);
        isSelectingCapture = true;

        overlayTilemap.ClearAllTiles();
        foreach (EnemyUnit enemy in capturableEnemies)
            overlayTilemap.SetTile(enemy.GridPosition, redOverlay);
    }
    public void CancelCaptureSelection()
    {
        isSelectingCapture = false;
        pendingCaptureUnit = null;
        capturableEnemies.Clear();
        overlayTilemap.ClearAllTiles();
    }

    public void StartDropTileSelection(PlayerUnit unit)
    {
        pendingDropUnit = unit;
        isSelectingDropTile = true;
        validDropTiles.Clear();

        overlayTilemap.ClearAllTiles();
        Vector3Int pos = unit.GridPosition;
        Vector3Int[] neighborCoords = {pos + Vector3Int.up, pos + Vector3Int.down, pos + Vector3Int.left, pos + Vector3Int.right };
        foreach (Vector3Int neighborCoord in neighborCoords)
        {
            bool occupied = MapManager.Instance.GetObjectsAt(neighborCoord).Any(o => o is PlayerUnit || o is EnemyUnit);
            bool walkable = PathfinderController.Instance.GetNode(neighborCoord)?.walkable ?? false;
            if (!occupied && walkable)
            {
                validDropTiles.Add(neighborCoord);
                overlayTilemap.SetTile(neighborCoord, greenOverlay);
            }
        }
    }
    public void CancelDropSelection()
    {
        isSelectingDropTile = false;
        pendingDropUnit = null;
        validDropTiles.Clear();
        overlayTilemap.ClearAllTiles();
    }


    // 
    // BEHAVIOR TREE FOR LEFT CLICKS
    // 
    private void OnClick(InputAction.CallbackContext context)
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

        if (GameController.Instance.gamePhase == GamePhase.Enemy) return;
        if (ScreenManager.Instance != null && ScreenManager.Instance.IsBlocking) return;
        if (InventoryMenuController.Instance != null && InventoryMenuController.Instance.isMenuOpen) return;
        if (TradeMenuController.Instance != null && TradeMenuController.Instance.isMenuOpen) return;
        

        //Player is selecting an enemy to attack
        if (isSelectingAttack)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
            if (clickedEnemy != null && attackableEnemies.Contains(clickedEnemy))
            {
                PlayerUnit attacker = pendingAttackUnit;
                WeaponInstance weapon = (forecastWeapon != null && lastForecastTarget == clickedEnemy)
                    ? forecastWeapon : attacker.inventory.EquippedWeapon;
                CancelAttackSelection();
                attacker.inventory.Equip(weapon);
                StartCoroutine(AttackThenDeactivate(attacker, clickedEnemy, weapon));
            }
            return;
        }

        if (isSelectingTradePartner)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            PlayerUnit clickedPartner = objectsAtTile.OfType<PlayerUnit>().FirstOrDefault();
            if (clickedPartner != null && tradablePartners.Contains(clickedPartner))
            {
                PlayerUnit trader = pendingTradeUnit;
                CancelTradeSelection();
                TradeMenuController.Instance.Show(trader, clickedPartner);
            }
            return;
        }

        if (isSelectingRescue)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            PlayerUnit clickedAlly = objectsAtTile.OfType<PlayerUnit>().FirstOrDefault();
            if (clickedAlly != null && rescuableAllies.Contains(clickedAlly))
            {
                PlayerUnit rescuer = pendingRescueUnit;
                CancelRescueSelection();
                ActionMenuController.Instance.CompleteRescue(rescuer, clickedAlly);
            }
            return;
        }

        if (isSelectingCapture)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
            EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
            if (clickedEnemy != null && capturableEnemies.Contains(clickedEnemy))
            {
                PlayerUnit capturer = pendingCaptureUnit;
                WeaponInstance equipped = capturer.inventory.EquippedWeapon;
                WeaponInstance weapon = (equipped != null && equipped.CanHitAt(1)) ? equipped : capturer.inventory.items.OfType<WeaponInstance>().FirstOrDefault(w => w.CanHitAt(1));
                CancelCaptureSelection();
                capturer.inventory.Equip(weapon);
                StartCoroutine(CaptureThenDeactivate(capturer, clickedEnemy, weapon));
            }
            return;
        }

        if (isSelectingDropTile)
        {
            Vector3Int clickedGrid = MapManager.Instance.WorldToGrid(worldPosition);
            if(validDropTiles.Contains(clickedGrid))
            {
                PlayerUnit dropper = pendingDropUnit;
                CancelDropSelection();
                dropper.dropUnit(clickedGrid);
                ActionMenuController.Instance.CompleteDrop();
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
                WeaponInstance weapon = (forecastWeapon != null && lastForecastTarget == clickedEnemy) ? forecastWeapon : null;
                Node bestTile = (forecastBestTile != null && lastForecastTarget == clickedEnemy) ? forecastBestTile : null;
                if (weapon == null || bestTile == null)
                {
                    MovementClass mc2 = currentMover.playerUnit.unitAttributes.movementClass;
                    List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerGrid, currentMover.playerUnit.unitAttributes.movement, mc2);
                    (weapon, bestTile) = FindBestAttackOption(currentMover.playerUnit, clickedEnemy, reachable);
                }
                if (weapon != null && bestTile != null)
                {
                    MovementClass mc = currentMover.playerUnit.unitAttributes.movementClass;
                    currentMover.playerUnit.inventory.Equip(weapon);
                    List<Node> path = PathfinderController.Instance.FindPath(playerGrid, bestTile.gridPosition, mc);
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

    // 
    // BEHAVIOR TREE FOR RIGHT CLICKS
    // 
    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (TradeMenuController.Instance != null && TradeMenuController.Instance.isMenuOpen)
        {
            TradeMenuController.Instance.Close();
        }
        else if (isSelectingTradePartner)
        {
            CancelTradeSelection();
            ActionMenuController.Instance.ReopenMenu();
        }
        else if (isSelectingDropTile)
        {
            CancelDropSelection();
            ActionMenuController.Instance.ReopenMenu();
        }
        else if (isSelectingRescue)
        {
            CancelRescueSelection();
            ActionMenuController.Instance.ReopenMenu();
        }
        else if (isSelectingAttack)
        {
            PlayerUnit unit = pendingAttackUnit;
            CancelAttackSelection();
            unit.mover.CancelMove();
            currentMover = unit.mover;
        }
        else if (isSelectingCapture)
        {
            PlayerUnit unit = pendingCaptureUnit;
            CancelCaptureSelection();
            unit.mover.CancelMove();
            currentMover = unit.mover;
        }
        else if (InventoryMenuController.Instance != null && InventoryMenuController.Instance.isSubMenuOpen)
        {
            InventoryMenuController.Instance.CloseSubMenu();
        }
        else if (InventoryMenuController.Instance != null && InventoryMenuController.Instance.isMenuOpen)
        {
            InventoryMenuController.Instance.Close();
        }
        else if (ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        {
            ActionMenuController.Instance.CancelMenu();
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

        if (GameController.Instance.gamePhase == GamePhase.Player)
        {
            UpdateHoverInfo(worldPosition);
            UpdateCombatForecast(worldPosition);
        }
        else
        {
            HoverInfoController.Instance?.Hide();
            CombatForecastController.Instance?.Hide();
        }

        if ((InventoryMenuController.Instance != null && InventoryMenuController.Instance.isMenuOpen)
            || (ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
            || (TradeMenuController.Instance != null && TradeMenuController.Instance.isMenuOpen)
            || isSelectingTradePartner || isSelectingRescue || isSelectingCapture || isSelectingDropTile)
        {
            reticalTransform.gameObject.SetActive(false);
        }
        else if (currentMover)
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
                if (forecastBestTile != null)
                    DrawPath(forecastBestTile.gridPosition);
                else
                    ClearLine();
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
            reticalTransform.gameObject.SetActive(true);
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
            foreach (ItemInstance item in currentMover.playerUnit.inventory.items)
            {
                if (item is not WeaponInstance weapon) continue;
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

    private void OnScroll(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (scroll > 0) weaponCycleIndex++;
        else if (scroll < 0) weaponCycleIndex--;
    }

    private void UpdateCombatForecast(Vector3 worldPosition)
    {
        List<MapObject> objects = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
        EnemyUnit hoveredEnemy = objects.OfType<EnemyUnit>().FirstOrDefault();

        if (hoveredEnemy != lastForecastTarget)
        {
            weaponCycleIndex = 0;
            lastForecastTarget = hoveredEnemy;
        }

        if (isSelectingAttack)
        {
            if (hoveredEnemy != null && attackableEnemies.Contains(hoveredEnemy))
            {
                int dist = Mathf.Abs(pendingAttackUnit.GridPosition.x - hoveredEnemy.GridPosition.x)
                         + Mathf.Abs(pendingAttackUnit.GridPosition.y - hoveredEnemy.GridPosition.y);
                List<WeaponInstance> valid = GetWeaponsInRange(pendingAttackUnit, dist);
                if (valid.Count > 0)
                {
                    weaponCycleIndex = ((weaponCycleIndex % valid.Count) + valid.Count) % valid.Count;
                    forecastWeapon = valid[weaponCycleIndex];
                    forecastBestTile = null;
                    CombatForecastController.Instance?.Show(pendingAttackUnit, hoveredEnemy, forecastWeapon);
                    return;
                }
            }
            forecastWeapon = null; forecastBestTile = null;
            CombatForecastController.Instance?.Hide();
            return;
        }

        if (currentMover != null && !currentMover.isMoving && hoveredEnemy != null)
        {
            Vector3Int playerGrid = tilemap.WorldToCell(currentMover.transform.position);
            List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerGrid, currentMover.playerUnit.unitAttributes.movement, currentMover.playerUnit.unitAttributes.movementClass);
            List<(WeaponInstance weapon, Node tile)> valid = GetWeaponOptionsForTarget(currentMover.playerUnit, hoveredEnemy, reachable);
            if (valid.Count > 0)
            {
                weaponCycleIndex = ((weaponCycleIndex % valid.Count) + valid.Count) % valid.Count;
                forecastWeapon = valid[weaponCycleIndex].weapon;
                forecastBestTile = valid[weaponCycleIndex].tile;
                CombatForecastController.Instance?.Show(currentMover.playerUnit, hoveredEnemy, forecastWeapon, forecastBestTile.gridPosition);
                return;
            }
        }

        forecastWeapon = null; forecastBestTile = null;
        CombatForecastController.Instance?.Hide();
    }

    private List<WeaponInstance> GetWeaponsInRange(PlayerUnit unit, int dist)
    {
        var result = new List<WeaponInstance>();
        foreach (ItemInstance item in unit.inventory.items)
            if (item is WeaponInstance w && w.CanHitAt(dist)) result.Add(w);
        return result;
    }

    private List<(WeaponInstance weapon, Node tile)> GetWeaponOptionsForTarget(PlayerUnit unit, EnemyUnit target, List<Node> reachable)
    {
        var result = new List<(WeaponInstance, Node)>();
        foreach (ItemInstance item in unit.inventory.items)
        {
            if (item is not WeaponInstance weapon) continue;
            Node best = FindBestTileForWeapon(unit, target, weapon, reachable);
            if (best != null) result.Add((weapon, best));
        }
        return result;
    }

    private Node FindBestTileForWeapon(PlayerUnit unit, EnemyUnit target, WeaponInstance weapon, List<Node> reachable)
    {
        int bestScore = int.MinValue;
        Node bestTile = null;
        Vector3Int targetPos = target.GridPosition;
        foreach (Node node in reachable)
        {
            var occupants = MapManager.Instance.GetObjectsAt(node.gridPosition);
            if (occupants.Any(o => o is PlayerUnit p && p != unit || o is EnemyUnit)) continue;
            int dist = Mathf.Abs(node.gridPosition.x - targetPos.x) + Mathf.Abs(node.gridPosition.y - targetPos.y);
            if (!weapon.CanHitAt(dist)) continue;
            CombatPreview preview = CombatCalculator.Preview(unit, target, weapon, node.gridPosition);
            int score = preview.damageDealt + (preview.killsDefender ? 100 : 0) + (!preview.defenderCanCounter ? 1000 : 0);
            if (score > bestScore) { bestScore = score; bestTile = node; }
        }
        return bestTile;
    }

    private IEnumerator AttackThenDeactivate(PlayerUnit attacker, EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon != null)
            yield return StartCoroutine(attacker.AttackCoroutine(target, weapon));
        if (attacker != null) attacker.SetInactive();
    }

    private void UpdateHoverInfo(Vector3 worldPosition)
    {
        List<MapObject> objects = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
        Unit hoveredUnit = objects.OfType<Unit>().FirstOrDefault();

        if (isSelectingAttack)
        {
            if (hoveredUnit is EnemyUnit hoveredEnemy && attackableEnemies.Contains(hoveredEnemy))
                HoverInfoController.Instance?.ShowUnit(hoveredEnemy);
            else
                HoverInfoController.Instance?.ShowUnit(pendingAttackUnit);
            return;
        }

        if (ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        {
            HoverInfoController.Instance?.ShowUnit(ActionMenuController.Instance.PendingUnit);
            return;
        }

        if (currentMover != null)
        {
            PlayerUnit selected = currentMover.playerUnit;
            if (hoveredUnit is EnemyUnit hoveredEnemy)
            {
                List<Node> reachable = PathfinderController.Instance.GetReachableNodes(
                    currentMover.transform.position,
                    selected.unitAttributes.movement,
                    selected.unitAttributes.movementClass);
                var (weapon, _) = FindBestAttackOption(selected, hoveredEnemy, reachable);
                if (weapon != null)
                {
                    HoverInfoController.Instance?.ShowUnit(hoveredEnemy);
                    return;
                }
            }
            HoverInfoController.Instance?.ShowUnit(selected);
            return;
        }

        foreach (PlayerUnit pu in FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None))
        {
            if (pu.mover != null && pu.mover.isMoving)
            {
                HoverInfoController.Instance?.ShowUnit(pu);
                return;
            }
        }

        if (hoveredUnit != null)
        {
            HoverInfoController.Instance?.ShowUnit(hoveredUnit);
            return;
        }
        Node node = PathfinderController.Instance.GetNode(worldPosition);
        if (node?.terrainType != null)
        {
            HoverInfoController.Instance?.ShowTerrain(node.terrainType);
            return;
        }
        HoverInfoController.Instance?.Hide();
    }

    private (WeaponInstance weapon, Node tile) FindBestAttackOption(PlayerUnit unit, EnemyUnit target, List<Node> reachable)
    {
        int bestScore = int.MinValue;
        WeaponInstance bestWeapon = null;
        Node bestTile = null;
        Vector3Int targetPosition = target.GridPosition;

        foreach(Node node in reachable)
        {
            var occupants = MapManager.Instance.GetObjectsAt(node.gridPosition);
            if (occupants.Any(o => o is PlayerUnit p && p != unit || o is EnemyUnit)) continue;

            foreach (ItemInstance item in unit.inventory.items)
            {
                if (item is not WeaponInstance weapon) continue;
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

    private IEnumerator CaptureThenDeactivate(PlayerUnit capturer, EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon != null)
        yield return StartCoroutine(capturer.CaptureCoroutine(target, weapon));
        if (capturer != null) capturer.SetInactive();
    }
}
