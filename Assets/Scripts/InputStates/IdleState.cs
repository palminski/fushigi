using System.Linq;
using UnityEngine;

public class IdleState : InputState
{
    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.ShowReticle(hoveredCell);

        Unit hoveredUnit = MapManager.Instance.GetObjectsAt(hoveredCell).OfType<Unit>().FirstOrDefault();
        if (hoveredUnit == null)
        {
            MapOverlay.Instance.ClearSelection();
            return;
        }

        var (reachable, attackable) = PathfinderController.Instance.GetUnitRange(hoveredUnit);
        OverlayColor moveColor = hoveredUnit is PlayerUnit ? OverlayColor.Green : OverlayColor.RedLight;
        MapOverlay.Instance.ShowRange(reachable,attackable,moveColor,OverlayColor.Red);
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
