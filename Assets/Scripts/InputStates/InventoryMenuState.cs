using UnityEngine;

// A unit's inventory is open (from its action menu). The menu's buttons handle
// equipping, using and discarding; this state handles going back with right-click.
public class InventoryMenuState : InputState
{
    public PlayerUnit Unit { get; }

    public InventoryMenuState(PlayerUnit unit)
    {
        Unit = unit;
    }

    public override void Enter()
    {
        InventoryMenu.Instance.Show(Unit);
    }

    public override void Exit()
    {
        InventoryMenu.Instance.Hide();
    }

    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.HideReticle();
        HoverInfoPanel.Instance?.ShowUnit(Unit);
    }

    // Map clicks are ignored - they're the same clicks that press the menu's buttons

    // Back out one level: the item's submenu first, then the inventory itself
    public override void OnRightClick()
    {
        if (InventoryMenu.Instance.isSubMenuOpen) InventoryMenu.Instance.CloseSubMenu();
        else InputController.Instance.OpenActionMenu(Unit);
    }

    public override string ToString() => $"Inventory for {Unit.name}";
}
