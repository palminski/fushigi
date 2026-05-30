using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ActionMenuController : MonoBehaviour
{
    public static ActionMenuController Instance { get; private set; }

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button waitButton;
    private PlayerUnit pendingUnit;
    private List<EnemyUnit> attackableEnemies = new List<EnemyUnit>();
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
        Vector3 screenPostion = Camera.main.WorldToScreenPoint(movedUnit.transform.position);
        menuPanel.GetComponent<RectTransform>().position = screenPostion + new Vector3(35f, 0f, 0f);

        attackButton.gameObject.SetActive(attackableEnemies.Count > 0);
        menuPanel.SetActive(true);
    }

    public void HideMenu()
    {
        menuPanel.SetActive(false);
        pendingUnit = null;
        attackableEnemies.Clear();
    }

    public void CancelMenu()
    {
        PlayerUnit unit = pendingUnit;
        HideMenu();
        unit.mover.CancelMove();
        InputController.Instance.currentMover = unit.mover;
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
