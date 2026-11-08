using System.Linq;
using UnityEngine;

public class IdleState : InputState
{
    public override void Pause()
    {
        MapOverlay.Instance.ClearSelection();
    }
    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.ShowReticle(hoveredCell);

        ShowHoveredUnitRange(hoveredCell);
        UpdateHoverInfo(hoveredCell);
    }

    private void ShowHoveredUnitRange(Vector3Int hoveredCell)
    {
        Unit hoveredUnit = MapManager.Instance.GetObjectsAt(hoveredCell).OfType<Unit>().FirstOrDefault();
        if (hoveredUnit == null)
        {
            MapOverlay.Instance.ClearSelection();
            return;
        }

        var (reachable, attackable) = Pathfinder.Instance.GetUnitRange(hoveredUnit);
        OverlayColor moveColor = hoveredUnit is PlayerUnit ? OverlayColor.GreenLight : OverlayColor.RedLight;
        MapOverlay.Instance.ShowRange(reachable,attackable,moveColor,OverlayColor.Red);
    }

    private void UpdateHoverInfo(Vector3Int hoveredCell)
    {
        // The hover panel stays hidden during the enemy's turn
        if (GamePhaseManager.Instance.gamePhase == GamePhase.Enemy)
        {
            HoverInfoPanel.Instance?.Hide();
            return;
        }

        // While a player unit walks to its destination, keep showing it
        PlayerUnit movingUnit = Object.FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None).FirstOrDefault(p => p.mover != null && p.mover.isMoving);
        if (movingUnit != null)
        {
            HoverInfoPanel.Instance?.ShowUnit(movingUnit);
            return;
        }

        ShowHoverInfoAt(hoveredCell);
    }

    public override void OnClick(Vector3Int clickedCell)
    {
        MapManager.Instance.RefreshMap();
        Unit clickedUnit = MapManager.Instance.GetObjectsAt(clickedCell).OfType<Unit>().FirstOrDefault();

        if (clickedUnit is PlayerUnit player && player.canAct && player.mover != null && !player.mover.isMoving)
        {
            InputController.Instance.ChangeState(new UnitSelectedState(player.mover));
        }
        else if (clickedUnit is EnemyUnit enemy)
        {
            InputController.Instance.ToggleDangerZone(enemy);
        }
    }

    public override string ToString()
    {
        return "Idle";
    }
}
