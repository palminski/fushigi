using System.Collections.Generic;
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
    private PlayerUnit pendingUnit;
    private List<EnemyUnit> attackableEnemies = new List<EnemyUnit>();
    private List<PlayerUnit> adjacentPlayers = new List<PlayerUnit>();
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
        menuPanel.SetActive(true);
    }

    public void HideMenu()
    {
        menuPanel.SetActive(false);
        pendingUnit = null;
        attackableEnemies.Clear();
        adjacentPlayers.Clear();
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
    public void OnWaitClicked()
    {
        PlayerUnit unit = pendingUnit;
        HideMenu();
        unit.SetInactive();
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
            foreach(Node node in attackableTiles)
            {
                foreach(MapObject obj in MapManager.Instance.GetObjectsAt(node.gridPosition))
                {
                    if (obj is EnemyUnit enemy) enemies.Add(enemy);
                }
            }
        }
        return new List<EnemyUnit>(enemies);
    }
}
