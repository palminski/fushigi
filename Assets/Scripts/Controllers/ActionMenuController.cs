using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenuController : MonoBehaviour
{
    public static ActionMenuController Instance { get; private set; }

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button waitButton;

    [SerializeField] private Button rescueButton;
    [SerializeField] private Button captureButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button takeButton;
    [SerializeField] private Button heldTradeButton;
    private PlayerUnit pendingUnit;
    private List<EnemyUnit> attackableEnemies = new List<EnemyUnit>();
    private List<PlayerUnit> adjacentPlayers = new List<PlayerUnit>();
    private List<PlayerUnit> rescuableAllies = new List<PlayerUnit>();
    private List<PlayerUnit> takeableAllies = new List<PlayerUnit>();
    private List<EnemyUnit> capturableEnemies = new List<EnemyUnit>();
    private bool canCancel = true;
    public bool isMenuOpen => menuPanel != null && menuPanel.activeSelf;
    public PlayerUnit PendingUnit => pendingUnit;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        menuPanel.SetActive(false);
    }

    public void ShowMenu(PlayerUnit movedUnit)
    {
        pendingUnit = movedUnit;
        attackableEnemies = FindAttackableEnemies(movedUnit);
        adjacentPlayers = FindAdjacentPlayers(movedUnit);
        Vector3 screenPostion = Camera.main.WorldToScreenPoint(movedUnit.transform.position);
        menuPanel.GetComponent<RectTransform>().position = screenPostion + new Vector3(35f, 0f, 0f);

        attackButton.gameObject.SetActive(attackableEnemies.Count > 0);
        inventoryButton.gameObject.SetActive(movedUnit.inventory.items.Count > 0);
        tradeButton.gameObject.SetActive(adjacentPlayers.Count > 0);
        RefreshUnitHoldButtons();
        menuPanel.SetActive(true);
    }

    private void RefreshUnitHoldButtons()
    {
        rescuableAllies = FindRescuableAllies(pendingUnit);
        capturableEnemies = FindCapturableEnemies(pendingUnit);
        takeableAllies = FindTakeableAllies(pendingUnit);

        rescueButton.gameObject.SetActive(pendingUnit.heldUnit == null && rescuableAllies.Count > 0 && pendingUnit.hasDroppedUnit == false);
        captureButton.gameObject.SetActive(pendingUnit.heldUnit == null && capturableEnemies.Count > 0);
        takeButton.gameObject.SetActive(pendingUnit.heldUnit == null && takeableAllies.Count > 0 && pendingUnit.hasDroppedUnit == false);
        dropButton.gameObject.SetActive(pendingUnit.heldUnit != null && pendingUnit.hasPickedUpUnit == false && ValidDropTileExists(pendingUnit));
        heldTradeButton.gameObject.SetActive(pendingUnit.heldUnit != null);
    }

    public void HideMenu()
    {
        menuPanel.SetActive(false);
        pendingUnit = null;
        attackableEnemies.Clear();
        adjacentPlayers.Clear();
        rescuableAllies.Clear();
        capturableEnemies.Clear();
        canCancel = true;
    }

    public void CancelMenu()
    {
        if (!canCancel) return;
        PlayerUnit unit = pendingUnit;
        HideMenu();
        unit.mover.CancelMove();
        InputController.Instance.currentMover = unit.mover;
    }

    public void ReopenMenu()
    {
        if (pendingUnit != null) menuPanel.SetActive(true);
    }

    public void ReopenMenuLocked()
    {
        canCancel = false;
        if (pendingUnit != null) menuPanel.SetActive(true);
    }

    public void OnTradeClicked()
    {
        FindAdjacentPlayers(pendingUnit);
        if (adjacentPlayers.Count == 0) return;
        menuPanel.SetActive(false);
        if (adjacentPlayers.Count == 1)
        {
            TradeMenuController.Instance.Show(pendingUnit, adjacentPlayers[0]);
        }
        else
        {
            InputController.Instance.StartTradePartnerSelection(pendingUnit, adjacentPlayers);
        }
    }

    public void OnInventoryClicked()
    {
        menuPanel.SetActive(false);
        InventoryMenuController.Instance.Show(pendingUnit);
    }

    public void OnAttackClicked()
    {
        InputController.Instance.StartAttackTargetSelection(pendingUnit, attackableEnemies);
        menuPanel.SetActive(false);
        pendingUnit = null;
    }
    public void OnRescueClicked()
    {
        if (rescuableAllies.Count == 0) return;
        menuPanel.SetActive(false);
        if (rescuableAllies.Count == 1)
        {
            CompleteRescue(pendingUnit, rescuableAllies[0]);
        }
        else
        {
            InputController.Instance.StartRescueTargetSelection(pendingUnit, rescuableAllies);
        }
    }
    public void OnTakeClicked()
    {
        if(takeableAllies.Count == 0) return;
        menuPanel.SetActive(false);
        if (takeableAllies.Count == 1)
        {
            CompleteTake(pendingUnit, takeableAllies[0]);
        }
        else
        {
            InputController.Instance.StartTakeTargetSelection(pendingUnit, takeableAllies);
        }
    }
    public void OnCaptureClicked()
    {
        if (capturableEnemies.Count == 0) return;
        InputController.Instance.StartCaptureTargetSelection(pendingUnit, capturableEnemies);
        menuPanel.SetActive(false);
        pendingUnit = null;
    }
    public void OnDropClicked()
    {
        if (pendingUnit.heldUnit == null) return;
        if (pendingUnit.heldUnit is PlayerUnit)
        {
            menuPanel.SetActive(false);
            InputController.Instance.StartDropTileSelection(pendingUnit);
        }
        else if (pendingUnit.heldUnit is EnemyUnit)
        {
            //Maybe this should spend whole turn?
            pendingUnit.ReleaseHeld();
            RefreshUnitHoldButtons();
            ReopenMenuLocked();
        }
    }
    public void OnHeldTradeClicked()
    {
        if (pendingUnit.heldUnit == null) return;
        menuPanel.SetActive(false);
        TradeMenuController.Instance.Show(pendingUnit, pendingUnit.heldUnit);
    }

    public void OnWaitClicked()
    {
        PlayerUnit unit = pendingUnit;
        HideMenu();
        unit.SetInactive();
    }

    public void CompleteRescue(PlayerUnit rescuer, PlayerUnit ally)
    {
        rescuer.PickUpUnit(ally);
        RefreshUnitHoldButtons();
        ReopenMenuLocked();
    }

    public void CompleteTake(PlayerUnit rescuer, PlayerUnit ally)
    {
        //ally refers to the unit holding the ally to be taken
        if(ally.heldUnit != null)
        {
            rescuer.TakeUnitFromAlly(ally);
        }
        RefreshUnitHoldButtons();
        ReopenMenuLocked();
    }

    public void CompleteDrop()
    {
        RefreshUnitHoldButtons();
        ReopenMenuLocked();
    }

    private List<PlayerUnit> FindAdjacentPlayers(PlayerUnit unit)
    {
        var result = new List<PlayerUnit>();
        Vector3Int pos = unit.GridPosition;
        Vector3Int[] neighbors = { pos + Vector3Int.up, pos + Vector3Int.down, pos + Vector3Int.left, pos + Vector3Int.right };
        foreach (Vector3Int n in neighbors)
            foreach (MapObject obj in MapManager.Instance.GetObjectsAt(n))
                if (obj is PlayerUnit other && other != unit) result.Add(other);
        return result;
    }

    private List<EnemyUnit> FindAttackableEnemies(PlayerUnit unit)
    {
        var enemies = new HashSet<EnemyUnit>();
        foreach (ItemInstance item in unit.inventory.items)
        {
            if (item is not WeaponInstance weapon) continue;
            List<Node> attackableTiles = PathfinderController.Instance.GetAttackableTiles(
                unit.transform.position, weapon.minRange, weapon.maxRange
            );
            foreach (Node node in attackableTiles)
            {
                foreach (MapObject obj in MapManager.Instance.GetObjectsAt(node.gridPosition))
                {
                    if (obj is EnemyUnit enemy) enemies.Add(enemy);
                }
            }
        }
        return new List<EnemyUnit>(enemies);
    }

    private List<PlayerUnit> FindRescuableAllies(PlayerUnit unit)
    {
        var result = new List<PlayerUnit>();
        foreach (PlayerUnit otherUnit in FindAdjacentPlayers(unit))
        {
            if (otherUnit.heldUnit == null && unit.unitAttributes.build > otherUnit.unitAttributes.build) result.Add(otherUnit);
        }
        return result;
    }

    private List<PlayerUnit> FindTakeableAllies(PlayerUnit unit)
    {
        var result = new List<PlayerUnit>();
        foreach (PlayerUnit otherUnit in FindAdjacentPlayers(unit))
        {
            if (otherUnit.heldUnit != null && unit.unitAttributes.build > otherUnit.heldUnit.unitAttributes.build) result.Add(otherUnit);
        }
        return result;
    }

    private List<EnemyUnit> FindCapturableEnemies(PlayerUnit unit)
    {
        var enemies = new List<EnemyUnit>();

        bool hasWeaponsThatCanCaptureAtRangeOne = unit.inventory.items.OfType<WeaponInstance>().Any(w => w.CanHitAt(1));
        if (!hasWeaponsThatCanCaptureAtRangeOne) return enemies;

        Vector3Int position = unit.GridPosition;
        Vector3Int[] neighborCoords = { position + Vector3Int.up, position + Vector3Int.down, position + Vector3Int.right, position + Vector3Int.left };
        foreach (Vector3Int neighborCoord in neighborCoords)
        {
            foreach (MapObject mapObject in MapManager.Instance.GetObjectsAt(neighborCoord))
            {
                if (mapObject is EnemyUnit enemy && enemy.unitAttributes.build < unit.unitAttributes.build) enemies.Add(enemy);
            }
        }
        return enemies;
    }

    private bool ValidDropTileExists(PlayerUnit unit)
    {
        Vector3Int position = unit.GridPosition;
        Vector3Int[] neighborCoords = { position + Vector3Int.up, position + Vector3Int.down, position + Vector3Int.right, position + Vector3Int.left };
        foreach (Vector3Int neighborCoord in neighborCoords)
        {
            bool occupied = MapManager.Instance.GetObjectsAt(neighborCoord).Any(o => o is Unit);
            bool walkable = PathfinderController.Instance.GetNode(neighborCoord)?.walkable ?? false;
            if(!occupied && walkable) return true;
        }
        return false;
    }
}
