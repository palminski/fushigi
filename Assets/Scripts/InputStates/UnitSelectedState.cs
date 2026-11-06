using UnityEngine;
using System.Collections.Generic;
using System.Linq;

//Unit is selected and the player is choosing where to move or who to attack
public class UnitSelectedState : InputState
{
    InputController inputController = InputController.Instance;
    public Mover Mover { get; }
    private readonly AttackForecast forecast = new AttackForecast();

    public UnitSelectedState(Mover mover)
    {
        Mover = mover;
    }

    public override void Exit()
    {
        MapOverlay.Instance.ClearSelection();
        MapOverlay.Instance.ClearPath();
        forecast.HidePanel();
    }

    // Every frame: show this unit's range, and the path it would take to the hovered cell
    public override void Tick(Vector3Int hoveredCell)
    {
        if (Mover.isMoving)
        {
            MapOverlay.Instance.HideReticle();
            return;
        }

        var (reachable, attackable) = Pathfinder.Instance.GetUnitRange(Mover.playerUnit);
        MapOverlay.Instance.ShowRange(reachable, attackable, OverlayColor.Green, OverlayColor.Red);
        MapOverlay.Instance.ShowReticle(hoveredCell);

        // Hovering an enemy: forecast the attack and show the path to the tile we'd attack from
        EnemyUnit hoveredEnemy = MapManager.Instance.GetObjectsAt(hoveredCell).OfType<EnemyUnit>().FirstOrDefault();
        if (hoveredEnemy != null)
        {
            var options = AttackPlanner.AttackOptions(Mover.playerUnit, hoveredEnemy, reachable.ToList());
            forecast.Show(Mover.playerUnit, hoveredEnemy, options);

            bool canAttack = options.Count > 0;
            HoverInfoPanel.Instance?.ShowUnit(canAttack ? hoveredEnemy : Mover.playerUnit);

            Node attackFrom = forecast.AttackFromFor(hoveredEnemy);
            if (attackFrom != null) ShowPathTo(attackFrom.gridPosition);
            else MapOverlay.Instance.ClearPath();
            return;
        }

        forecast.Clear();
        HoverInfoPanel.Instance?.ShowUnit(Mover.playerUnit);

        bool onMap = LevelMap.Instance.HasGround(hoveredCell);
        if (onMap && ShowPathTo(hoveredCell)) return;

        MapOverlay.Instance.HideReticle();
        MapOverlay.Instance.ClearPath();
    }

    // Draws the path to `cell` if this unit can reach it. Returns whether it could.
    private bool ShowPathTo(Vector3Int cell)
    {
        Vector3Int unitCell = Mover.playerUnit.GridPosition;
        MovementClass movementClass = Mover.playerUnit.unitAttributes.movementClass;
        List<Node> path = Pathfinder.Instance.FindPath(unitCell, cell, movementClass);
        bool canReach = path != null && Pathfinder.Instance.GetPathCost(path, movementClass) <= Mover.playerUnit.unitAttributes.movement;

        if (canReach) MapOverlay.Instance.ShowPath(unitCell, path);
        else MapOverlay.Instance.ClearPath();
        return canReach;
    }

    public override void OnClick(Vector3Int cell)
    {
        List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(cell);

        Vector3Int playerGridPos = LevelMap.Instance.WorldToCell(Mover.transform.position);
        
        if (playerGridPos == cell)
        {
            Mover.DeactivatePlayer();
            inputController.ReturnToIdle();
            return;
        }

        EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
        if (clickedEnemy != null && !Mover.isMoving)
        {
            WeaponInstance weapon = forecast.WeaponFor(clickedEnemy);
            Node bestTile = forecast.AttackFromFor(clickedEnemy);
            if (weapon == null || bestTile == null)
            {
                MovementClass mc2 = Mover.playerUnit.unitAttributes.movementClass;
                List<Node> reachable = Pathfinder.Instance.GetReachableNodes(playerGridPos, Mover.playerUnit.unitAttributes.movement, mc2);
                (weapon, bestTile) = AttackPlanner.BestAttack(Mover.playerUnit, clickedEnemy, reachable);
            }
            if (weapon != null && bestTile != null)
            {
                MovementClass mc = Mover.playerUnit.unitAttributes.movementClass;
                Mover.playerUnit.inventory.Equip(weapon);
                List<Node> path = Pathfinder.Instance.FindPath(playerGridPos, bestTile.gridPosition, mc);
                Mover.QueueAttack(clickedEnemy, weapon);
                inputController.ReturnToIdle();
                Mover.StartMoving(path, Mover.playerUnit.unitAttributes.movement);
            }
            return;
        }

        bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || obj is EnemyUnit);
        if (validTile && LevelMap.Instance.HasGround(cell))
        {

            MovementClass movementClass = Mover.playerUnit.unitAttributes.movementClass;
            List<Node> path = Pathfinder.Instance.FindPath(playerGridPos, cell, movementClass);
            if (path != null && path.Count > 0 && Pathfinder.Instance.GetPathCost(path, movementClass) <= Mover.playerUnit.unitAttributes.movement)
            {
                inputController.ReturnToIdle();
                Mover.StartMoving(path, Mover.playerUnit.unitAttributes.movement);
            }
        }
    }

    public override void OnRightClick()
    {
        inputController.ReturnToIdle();
    }

    public override void OnScroll(int direction)
    {
        forecast.Scroll(direction);
    }

    public override string ToString()
    {
        return $"{Mover.name} has been selected";
    }
}
