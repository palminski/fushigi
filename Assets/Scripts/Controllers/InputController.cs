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
    public HashSet<EnemyUnit> toggledEnemies = new HashSet<EnemyUnit>();
    public Mover currentMover => (currentState as UnitSelectedState)?.Mover;
    private GameInput inputActions;
    private InputState currentState;
    [SerializeField] private string currentStateName = "None"; // read-only, shown in the Inspector for debugging

    private int weaponCycleIndex = 0;
    public EnemyUnit lastForecastTarget = null;
    public WeaponInstance forecastWeapon = null;
    public Node forecastBestTile = null;

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

    // 
    // STATE MACHINE
    // 
    public void ChangeState(InputState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentStateName = newState?.ToString() ?? "None";
        currentState?.Enter();
    }

    // 
    // TARGET SELECTION - started from the action menu
    // 
    public void StartAttackTargetSelection(PlayerUnit unit, List<EnemyUnit> enemies)
    {
        ChangeState(new TargetSelectState(TargetKind.Attack, unit, CellsOf(enemies), OverlayColor.Red,
            onConfirm: cell =>
            {
                EnemyUnit target = UnitAt<EnemyUnit>(cell);
                WeaponInstance weapon = (forecastWeapon != null && lastForecastTarget == target) ? forecastWeapon : unit.inventory.EquippedWeapon;
                unit.inventory.Equip(weapon);
                StartCoroutine(AttackThenDeactivate(unit, target, weapon));
            },
            onCancel: () => ReturnToMovement(unit)));
    }

    public void StartCaptureTargetSelection(PlayerUnit unit, List<EnemyUnit> enemies)
    {
        ChangeState(new TargetSelectState(TargetKind.Capture, unit, CellsOf(enemies), OverlayColor.Red,
            onConfirm: cell =>
            {
                EnemyUnit target = UnitAt<EnemyUnit>(cell);
                WeaponInstance equipped = (forecastWeapon != null && lastForecastTarget == target) ? forecastWeapon : unit.inventory.EquippedWeapon;
                WeaponInstance weapon = (equipped != null && equipped.CanHitAt(1)) ? equipped : unit.inventory.items.OfType<WeaponInstance>().FirstOrDefault(w => w.CanHitAt(1));
                unit.inventory.Equip(weapon);
                StartCoroutine(CaptureThenDeactivate(unit, target, weapon));
            },
            onCancel: () => ReturnToMovement(unit)));
    }

    public void StartTradePartnerSelection(PlayerUnit unit, List<PlayerUnit> partners)
    {
        ChangeState(new TargetSelectState(TargetKind.Trade, unit, CellsOf(partners), OverlayColor.Green,
            onConfirm: cell => TradeMenuController.Instance.Show(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenuController.Instance.ReopenMenu()));
    }

    public void StartRescueTargetSelection(PlayerUnit unit, List<PlayerUnit> allies)
    {
        ChangeState(new TargetSelectState(TargetKind.Rescue, unit, CellsOf(allies), OverlayColor.Green,
            onConfirm: cell => ActionMenuController.Instance.CompleteRescue(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenuController.Instance.ReopenMenu()));
    }

    public void StartTakeTargetSelection(PlayerUnit unit, List<PlayerUnit> allies)
    {
        ChangeState(new TargetSelectState(TargetKind.Take, unit, CellsOf(allies), OverlayColor.Green,
            onConfirm: cell => ActionMenuController.Instance.CompleteTake(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenuController.Instance.ReopenMenu()));
    }

    public void StartDropTileSelection(PlayerUnit unit)
    {
        List<Vector3Int> dropTiles = new List<Vector3Int>();
        Vector3Int pos = unit.GridPosition;
        Vector3Int[] neighborCoords = {pos + Vector3Int.up, pos + Vector3Int.down, pos + Vector3Int.left, pos + Vector3Int.right };
        foreach (Vector3Int neighborCoord in neighborCoords)
        {
            bool occupied = MapManager.Instance.GetObjectsAt(neighborCoord).Any(o => o is PlayerUnit || o is EnemyUnit);
            bool walkable = PathfinderController.Instance.GetNode(neighborCoord)?.walkable ?? false;
            if (!occupied && walkable) dropTiles.Add(neighborCoord);
        }

        ChangeState(new TargetSelectState(TargetKind.Drop, unit, dropTiles, OverlayColor.Green,
            onConfirm: cell =>
            {
                unit.DropUnit(cell);
                ActionMenuController.Instance.CompleteDrop();
            },
            onCancel: () => ActionMenuController.Instance.ReopenMenu()));
    }

    // Undo the unit's move and let the player pick a new destination
    private void ReturnToMovement(PlayerUnit unit)
    {
        unit.mover.CancelMove();
        ChangeState(new UnitSelectedState(unit.mover));
    }

    private static List<Vector3Int> CellsOf(IEnumerable<Unit> units)
    {
        return units.Select(u => u.GridPosition).ToList();
    }

    private static T UnitAt<T>(Vector3Int cell) where T : Unit
    {
        return MapManager.Instance.GetObjectsAt(cell).OfType<T>().FirstOrDefault();
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
        

        //Player is picking a target (attack, capture, trade, rescue, take, drop) - the state handles it
        if (currentState != null)
        {
            currentState.OnClick(MapManager.Instance.WorldToGrid(worldPosition));
            return;
        }

        List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(MapManager.Instance.WorldToGrid(worldPosition));
        if (ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        {
            //If Unit clicked after moving act as if wait was clicked
            if (objectsAtTile.Contains(ActionMenuController.Instance.PendingUnit)) ActionMenuController.Instance.OnWaitClicked();
            return;
        }
        
            MapManager.Instance.RefreshMap();

            Unit hoveredUnit = objectsAtTile.OfType<Unit>().FirstOrDefault();
            if (hoveredUnit == null) return;
            if(hoveredUnit is PlayerUnit playerUnit)

            // PlayerUnit playerUnit = objectsAtTile.OfType<PlayerUnit>().FirstOrDefault();

            if (playerUnit.canAct)
            {
                Mover mover = playerUnit.mover;
                if (mover != null && !mover.isMoving)
                {
                    ChangeState(new UnitSelectedState(mover));
                }
                return;
            }
            if(hoveredUnit is EnemyUnit clickedEnemy)
            {
                if(!toggledEnemies.Remove(clickedEnemy)) toggledEnemies.Add(clickedEnemy);
                clickedEnemy.UpdateIndicators();
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
        else if (currentState != null)
        {
            currentState.OnRightClick();
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
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);

        RefreshRangeOverlay(gridPosition);

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
            || (TradeMenuController.Instance != null && TradeMenuController.Instance.isMenuOpen))
        {
            MapOverlay.Instance.HideReticle();
        }
        else if (currentState != null)
        {
            currentState.Tick(gridPosition);
        }
        else
        {
            MapOverlay.Instance.ShowReticle(gridPosition);
        }
    }

    private void RefreshRangeOverlay(Vector3Int hoveredCoord)
    {
        MapOverlay.Instance.ClearDangerZones();

        toggledEnemies.RemoveWhere(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        foreach(EnemyUnit enemy in toggledEnemies)
        {
            var (reachable, attackable) = PathfinderController.Instance.GetUnitRange(enemy);
            MapOverlay.Instance.AddDangerZone(reachable, attackable);
        }

        if (!IsBaseIdleState()) return;
        MapOverlay.Instance.ClearSelection();

        Unit hoveredUnit = MapManager.Instance.GetObjectsAt(hoveredCoord).OfType<Unit>().FirstOrDefault();
        if (hoveredUnit == null) return;
        bool hoveredUnitIsPlayer = hoveredUnit is PlayerUnit;
        var (hoveredReachable, hoveredAttackable) = PathfinderController.Instance.GetUnitRange(hoveredUnit);
        MapOverlay.Instance.ShowRange(hoveredReachable, hoveredAttackable, hoveredUnitIsPlayer ? OverlayColor.GreenLight : OverlayColor.RedLight, OverlayColor.Red);

    }

    private bool IsBaseIdleState()
    {
        return 
        !(InventoryMenuController.Instance != null && InventoryMenuController.Instance.isMenuOpen)
        && !(ActionMenuController.Instance != null && ActionMenuController.Instance.isMenuOpen)
        && !(TradeMenuController.Instance != null && TradeMenuController.Instance.isMenuOpen)
        && currentState == null;
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

        if (currentState is TargetSelectState targeting && targeting.IsCombat)
        {
            if (hoveredEnemy != null && targeting.IsTarget(hoveredEnemy.GridPosition))
            {
                PlayerUnit unit = targeting.Unit;
                int dist = Mathf.Abs(unit.GridPosition.x - hoveredEnemy.GridPosition.x)
                         + Mathf.Abs(unit.GridPosition.y - hoveredEnemy.GridPosition.y);
                List<WeaponInstance> valid = GetWeaponsInRange(unit, dist);
                if (valid.Count > 0)
                {
                    weaponCycleIndex = ((weaponCycleIndex % valid.Count) + valid.Count) % valid.Count;
                    forecastWeapon = valid[weaponCycleIndex];
                    forecastBestTile = null;
                    CombatForecastController.Instance?.Show(unit, hoveredEnemy, forecastWeapon, null, targeting.Kind == TargetKind.Capture);
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

        if (currentState is TargetSelectState targeting && targeting.IsCombat)
        {
            if (hoveredUnit is EnemyUnit hoveredEnemy && targeting.IsTarget(hoveredEnemy.GridPosition))
                HoverInfoController.Instance?.ShowUnit(hoveredEnemy);
            else
                HoverInfoController.Instance?.ShowUnit(targeting.Unit);
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

    public (WeaponInstance weapon, Node tile) FindBestAttackOption(PlayerUnit unit, EnemyUnit target, List<Node> reachable)
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
