using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }
    public HashSet<EnemyUnit> toggledEnemies = new HashSet<EnemyUnit>();
    private GameInput inputActions;
    private InputState currentState;
    [SerializeField] private string currentStateName = "None"; // read-only, shown in the Inspector for debugging

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

    void Start()
    {
        ReturnToIdle();
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
        AttackForecast forecast = new AttackForecast();
        ChangeState(new TargetSelectState(TargetKind.Attack, unit, CellsOf(enemies), OverlayColor.Red,
            onConfirm: cell =>
            {
                EnemyUnit target = UnitAt<EnemyUnit>(cell);
                WeaponInstance weapon = forecast.WeaponFor(target) ?? unit.inventory.EquippedWeapon;
                unit.inventory.Equip(weapon);
                StartCoroutine(unit.AttackAndEndTurn(target, weapon));
            },
            onCancel: () => ReturnToMovement(unit),
            forecast: forecast));
    }

    public void StartCaptureTargetSelection(PlayerUnit unit, List<EnemyUnit> enemies)
    {
        AttackForecast forecast = new AttackForecast();
        ChangeState(new TargetSelectState(TargetKind.Capture, unit, CellsOf(enemies), OverlayColor.Red,
            onConfirm: cell =>
            {
                EnemyUnit target = UnitAt<EnemyUnit>(cell);
                WeaponInstance equipped = forecast.WeaponFor(target) ?? unit.inventory.EquippedWeapon;
                WeaponInstance weapon = (equipped != null && equipped.CanHitAt(1)) ? equipped : unit.inventory.items.OfType<WeaponInstance>().FirstOrDefault(w => w.CanHitAt(1));
                unit.inventory.Equip(weapon);
                StartCoroutine(unit.CaptureAndEndTurn(target, weapon));
            },
            onCancel: () => ReturnToMovement(unit),
            forecast: forecast));
    }

    public void StartTradePartnerSelection(PlayerUnit unit, List<PlayerUnit> partners)
    {
        ChangeState(new TargetSelectState(TargetKind.Trade, unit, CellsOf(partners), OverlayColor.Green,
            onConfirm: cell => TradeMenu.Instance.Show(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenu.Instance.ReopenMenu()));
    }

    public void StartRescueTargetSelection(PlayerUnit unit, List<PlayerUnit> allies)
    {
        ChangeState(new TargetSelectState(TargetKind.Rescue, unit, CellsOf(allies), OverlayColor.Green,
            onConfirm: cell => ActionMenu.Instance.CompleteRescue(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenu.Instance.ReopenMenu()));
    }

    public void StartTakeTargetSelection(PlayerUnit unit, List<PlayerUnit> allies)
    {
        ChangeState(new TargetSelectState(TargetKind.Take, unit, CellsOf(allies), OverlayColor.Green,
            onConfirm: cell => ActionMenu.Instance.CompleteTake(unit, UnitAt<PlayerUnit>(cell)),
            onCancel: () => ActionMenu.Instance.ReopenMenu()));
    }

    public void StartDropTileSelection(PlayerUnit unit)
    {
        List<Vector3Int> dropTiles = new List<Vector3Int>();
        Vector3Int pos = unit.GridPosition;
        Vector3Int[] neighborCoords = {pos + Vector3Int.up, pos + Vector3Int.down, pos + Vector3Int.left, pos + Vector3Int.right };
        foreach (Vector3Int neighborCoord in neighborCoords)
        {
            bool occupied = MapManager.Instance.GetObjectsAt(neighborCoord).Any(o => o is PlayerUnit || o is EnemyUnit);
            bool walkable = Pathfinder.Instance.GetNode(neighborCoord)?.walkable ?? false;
            if (!occupied && walkable) dropTiles.Add(neighborCoord);
        }

        ChangeState(new TargetSelectState(TargetKind.Drop, unit, dropTiles, OverlayColor.Green,
            onConfirm: cell =>
            {
                unit.DropUnit(cell);
                ActionMenu.Instance.CompleteDrop();
            },
            onCancel: () => ActionMenu.Instance.ReopenMenu()));
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

        if (GamePhaseManager.Instance.gamePhase == GamePhase.Enemy) return;
        if (ScreenManager.Instance != null && ScreenManager.Instance.IsBlocking) return;
        if (InventoryMenu.Instance != null && InventoryMenu.Instance.isMenuOpen) return;
        if (TradeMenu.Instance != null && TradeMenu.Instance.isMenuOpen) return;
        

        Vector3Int cell = MapManager.Instance.WorldToGrid(worldPosition);

        if(ActionMenu.Instance != null && ActionMenu.Instance.isMenuOpen)
        {
            //Unit clicked after moving treat as wait
            if(MapManager.Instance.GetObjectsAt(cell).Contains(ActionMenu.Instance.PendingUnit)) ActionMenu.Instance.OnWaitClicked();
            return;
        }

        currentState.OnClick(cell);
    }

    // 
    // BEHAVIOR TREE FOR RIGHT CLICKS
    // 
    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (TradeMenu.Instance != null && TradeMenu.Instance.isMenuOpen)
        {
            TradeMenu.Instance.Close();
        }
        else if (InventoryMenu.Instance != null && InventoryMenu.Instance.isSubMenuOpen)
        {
            InventoryMenu.Instance.CloseSubMenu();
        }
        else if (InventoryMenu.Instance != null && InventoryMenu.Instance.isMenuOpen)
        {
            InventoryMenu.Instance.Close();
        }
        else if (ActionMenu.Instance != null && ActionMenu.Instance.isMenuOpen)
        {
            ActionMenu.Instance.CancelMenu();
        }
        else
        {
            currentState.OnRightClick();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

        Vector3Int gridPosition = LevelMap.Instance.WorldToCell(worldPosition);

        RefreshDangerZones();

        if(IsMenuOpen())
        {
            // The menus handle their own input; the hover panel shows whose menu it is
            MapOverlay.Instance.HideReticle();
            CombatForecastPanel.Instance?.Hide();
            PlayerUnit menuUnit = ActionMenu.Instance != null ? ActionMenu.Instance.PendingUnit : null;
            if (menuUnit != null) HoverInfoPanel.Instance?.ShowUnit(menuUnit);
            else HoverInfoPanel.Instance?.Hide();
        }
        else
        {
            currentState.Tick(gridPosition);
        }
    }

    private void RefreshDangerZones()
    {
        MapOverlay.Instance.ClearDangerZones();

        toggledEnemies.RemoveWhere(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        foreach(EnemyUnit enemy in toggledEnemies)
        {
            var (reachable, attackable) = Pathfinder.Instance.GetUnitRange(enemy);
            MapOverlay.Instance.AddDangerZone(reachable, attackable);
        }
    }

    

    private void OnScroll(InputAction.CallbackContext context)
    {
        float scroll = context.ReadValue<float>();
        if (scroll > 0) currentState?.OnScroll(1);
        else if (scroll < 0) currentState?.OnScroll(-1);
    }

    public void ReturnToIdle()
    {
        ChangeState(new IdleState());
    }
    public void ToggleDangerZone(EnemyUnit enemy)
    {
        if (!toggledEnemies.Remove(enemy)) toggledEnemies.Add(enemy);
        enemy.UpdateIndicators();
    }
    private bool IsMenuOpen()
    {
        return (InventoryMenu.Instance != null && InventoryMenu.Instance.isMenuOpen)
        || (ActionMenu.Instance != null && ActionMenu.Instance.isMenuOpen)
        || (TradeMenu.Instance != null && TradeMenu.Instance.isMenuOpen);
    }
}
