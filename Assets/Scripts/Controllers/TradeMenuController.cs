using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class TradeMenuController : MonoBehaviour
{
    public static TradeMenuController Instance { get; private set; }

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform leftItemParent;
    [SerializeField] private Transform rightItemParent;
    [SerializeField] private TextMeshProUGUI leftUnitNameText;
    [SerializeField] private TextMeshProUGUI rightUnitNameText;
    [SerializeField] private GameObject itemRowPrefab;
    [SerializeField] private RectTransform dragGhost;
    [SerializeField] private TextMeshProUGUI dragGhostText;

    private PlayerUnit initiator;
    private Unit partner;
    private HashSet<ItemInstance> initiatorSnapshot;
    private HashSet<ItemInstance> partnerSnapshot;
    private TradeItemRow selectedRow;
    private TradeItemRow dragSource;

    public bool isMenuOpen => menuPanel != null && menuPanel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        menuPanel.SetActive(false);
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
    }

    public void Show(PlayerUnit initiator, Unit partner)
    {
        this.initiator = initiator;
        this.partner = partner;
        initiatorSnapshot = new HashSet<ItemInstance>(initiator.inventory.items);
        partnerSnapshot = new HashSet<ItemInstance>(partner.inventory.items);
        selectedRow = null;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(initiator.transform.position);
        menuPanel.GetComponent<RectTransform>().position = screenPos + new Vector3(35f, 0f, 0f);

        leftUnitNameText.text = initiator.gameObject.name;
        rightUnitNameText.text = partner.gameObject.name;

        menuPanel.SetActive(true);
        Refresh();
    }

    public void Refresh()
    {
        BuildList(leftItemParent, initiator, true);
        BuildList(rightItemParent, partner, false);
        ClearSelection();
    }

    private void BuildList(Transform parent, Unit unit, bool isLeftSide)
    {
        foreach (Transform child in parent)
            Destroy(child.gameObject);
        foreach (ItemInstance item in unit.inventory.items)
        {
            var row = Instantiate(itemRowPrefab, parent).GetComponent<TradeItemRow>();
            row.Setup(item, isLeftSide);
        }
        if (unit.inventory.items.Count < unit.inventory.maxCapacity)
        {
            var emptyRow = Instantiate(itemRowPrefab, parent).GetComponent<TradeItemRow>();
            emptyRow.Setup(null, isLeftSide);
        }
    }

    public void OnItemClicked(TradeItemRow clicked)
    {
        if (clicked.item == null)
        {
            // Empty slot — give selected item here if it's from the other side
            if (selectedRow != null && selectedRow.isLeftSide != clicked.isLeftSide)
                PerformGive(selectedRow.item, selectedRow.isLeftSide);
            ClearSelection();
            return;
        }

        if (selectedRow == null)
        {
            selectedRow = clicked;
            clicked.SetSelected(true);
        }
        else if (selectedRow == clicked)
        {
            ClearSelection();
        }
        else if (selectedRow.isLeftSide == clicked.isLeftSide)
        {
            Unit unit = selectedRow.isLeftSide ? initiator : partner;
            int fromIdx = unit.inventory.items.IndexOf(selectedRow.item);
            int toIdx = unit.inventory.items.IndexOf(clicked.item);
            (unit.inventory.items[fromIdx], unit.inventory.items[toIdx]) = (unit.inventory.items[toIdx], unit.inventory.items[fromIdx]);
                ClearSelection();
            Refresh();
        }
        else
        {
            PerformSwap(selectedRow, clicked);
            ClearSelection();
        }
    }

    private void PerformSwap(TradeItemRow rowA, TradeItemRow rowB)
    {
        Unit unitA = rowA.isLeftSide ? initiator : partner;
        Unit unitB = rowB.isLeftSide ? initiator : partner;
        int idxA = unitA.inventory.items.IndexOf(rowA.item);
        int idxB = unitB.inventory.items.IndexOf(rowB.item);
        unitA.inventory.items[idxA] = rowB.item;
        unitB.inventory.items[idxB] = rowA.item;
        Refresh();
    }

    private void PerformGive(ItemInstance item, bool fromLeft)
    {
        Unit from = fromLeft ? initiator : partner;
        Unit to = fromLeft ? partner : initiator;
        if (to.inventory.items.Count >= to.inventory.maxCapacity) return;
        from.inventory.items.Remove(item);
        to.inventory.items.Add(item);
        Refresh();
    }

    public void OnDropOnRow(TradeItemRow target)
    {
        if (dragSource == null) return;
        if (dragSource == target) { EndDragCleanup(); return; }

        ItemInstance sourceItem = dragSource.item;
        bool sourceIsLeft = dragSource.isLeftSide;
        EndDragCleanup();

        if (target.item == null)
        {
            if (sourceIsLeft != target.isLeftSide)
                PerformGive(sourceItem, sourceIsLeft);
        }
        else if (sourceIsLeft == target.isLeftSide)
        {
            Unit unit = sourceIsLeft ? initiator : partner;
            int fromIdx = unit.inventory.items.IndexOf(sourceItem);
            int toIdx = unit.inventory.items.IndexOf(target.item);
            (unit.inventory.items[fromIdx], unit.inventory.items[toIdx]) = (unit.inventory.items[toIdx], unit.inventory.items[fromIdx]);
                Refresh();
        }
        else
        {
            Unit unitA = sourceIsLeft ? initiator : partner;
            Unit unitB = target.isLeftSide ? initiator : partner;
            int idxA = unitA.inventory.items.IndexOf(sourceItem);
            int idxB = unitB.inventory.items.IndexOf(target.item);
            unitA.inventory.items[idxA] = target.item;
            unitB.inventory.items[idxB] = sourceItem;
                Refresh();
        }
    }

    public void OnDropOnPanel(bool isLeftPanel)
    {
        if (dragSource == null) return;
        if (dragSource.isLeftSide == isLeftPanel) { EndDragCleanup(); return; }

        ItemInstance sourceItem = dragSource.item;
        bool sourceIsLeft = dragSource.isLeftSide;
        EndDragCleanup();
        PerformGive(sourceItem, sourceIsLeft);
    }

    public void BeginDrag(TradeItemRow source)
    {
        dragSource = source;
        if (dragGhost != null)
        {
            dragGhostText.text = GetItemLabel(source.item);
            dragGhost.gameObject.SetActive(true);
        }
    }

    public void UpdateDrag(Vector2 screenPos)
    {
        if (dragGhost != null) dragGhost.position = screenPos;
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (dragSource == null) { EndDragCleanup(); return; }

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        TradeItemRow targetRow = null;
        TradeInventoryPanel targetPanel = null;

        foreach (var result in results)
        {
            if (targetRow == null)
                targetRow = result.gameObject.GetComponent<TradeItemRow>();
            if (targetPanel == null)
                targetPanel = result.gameObject.GetComponentInParent<TradeInventoryPanel>();
        }

        if (targetRow != null && targetRow != dragSource)
            OnDropOnRow(targetRow);
        else if (targetPanel != null)
            OnDropOnPanel(targetPanel.isLeftSide);
        else
            EndDragCleanup();
    }

    private void EndDragCleanup()
    {
        if (dragSource != null)
        {
            var cg = dragSource.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = true;
        }
        dragSource = null;
        if (dragGhost != null) dragGhost.gameObject.SetActive(false);
    }

    private void ClearSelection()
    {
        if (selectedRow != null) selectedRow.SetSelected(false);
        selectedRow = null;
    }

    public void Close()
    {
        AutoEquip(initiator);
        AutoEquip(partner);

        PlayerUnit unit = initiator;
        bool itemsChanged = !initiatorSnapshot.SetEquals(initiator.inventory.items)
                         || !partnerSnapshot.SetEquals(partner.inventory.items);

        menuPanel.SetActive(false);
        initiator = null;
        partner = null;
        initiatorSnapshot = null;
        partnerSnapshot = null;

        if (itemsChanged)
            ActionMenuController.Instance.ReopenMenuLocked();
        else
            ActionMenuController.Instance.ReopenMenu();
    }

    private void AutoEquip(Unit unit)
    {
        WeaponInstance first = unit.inventory.items.OfType<WeaponInstance>().FirstOrDefault();
        if (first != null) unit.inventory.Equip(first);
    }

    public static string GetItemLabel(ItemInstance item)
    {
        if (item == null) return "";
        if (item is WeaponInstance w) return $"{w.data.itemName}  {w.currentDurability}/{w.WeaponData.maxDurability}";
        if (item is ConsumableInstance c) return $"{c.data.itemName}  +{c.ConsumableData.healAmount} HP";
        return item.data.itemName;
    }
}
