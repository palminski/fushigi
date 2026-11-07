using System.Linq;
using UnityEngine;

// The action menu is open for a unit that has just moved (or is waiting in place).
// The menu's buttons decide what the unit does; this state handles the map around it.
public class ActionMenuState : InputState
{
    public PlayerUnit Unit { get; }

    public ActionMenuState(PlayerUnit unit)
    {
        Unit = unit;
    }

    public override void Enter()
    {
        MapOverlay.Instance.ClearSelection();
        MapOverlay.Instance.ClearPath();
        ActionMenu.Instance.ShowMenu(Unit);
    }

    public override void Exit()
    {
        ActionMenu.Instance.HideMenu();
    }

    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.HideReticle();
        HoverInfoPanel.Instance?.ShowUnit(Unit);
    }

    // Clicking the unit itself is a shortcut for Wait. Other map clicks are ignored -
    // they're usually the same click that pressed one of the menu's buttons.
    public override void OnClick(Vector3Int cell)
    {
        if (MapManager.Instance.GetObjectsAt(cell).Contains(Unit)) ActionMenu.Instance.OnWaitClicked();
    }

    // Undo the move, unless the unit already did something this turn that locked it in place
    public override void OnRightClick()
    {
        if (Unit.mover.CanUndoMove) InputController.Instance.ReturnToMovement(Unit);
    }

    public override string ToString() => $"Action menu for {Unit.name}";
}
