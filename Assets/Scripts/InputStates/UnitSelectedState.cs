using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

//Unit is selected and the player is choosing where to move or who to attack
public class UnitSelectedState : InputState
{
    InputController inputController = InputController.Instance;
    public Mover Mover { get; }

    public UnitSelectedState(Mover mover)
    {
        Mover = mover;
    }

    public override void Exit()
    {
        MapOverlay.Instance.ClearSelection();
        MapOverlay.Instance.ClearPath();
    }

    // Every frame: show this unit's range, and the path it would take to the hovered cell
    public override void Tick(Vector3Int hoveredCell)
    {
        if (Mover.isMoving)
        {
            MapOverlay.Instance.HideReticle();
            return;
        }

        var (reachable, attackable) = PathfinderController.Instance.GetUnitRange(Mover.playerUnit);
        MapOverlay.Instance.ShowRange(reachable, attackable, OverlayColor.Green, OverlayColor.Red);
        MapOverlay.Instance.ShowReticle(hoveredCell);

        // Hovering an enemy: show the path to the tile we'd attack from
        EnemyUnit hoveredEnemy = MapManager.Instance.GetObjectsAt(hoveredCell).OfType<EnemyUnit>().FirstOrDefault();
        if (hoveredEnemy != null)
        {
            Node attackFrom = inputController.forecastBestTile;
            if (attackFrom != null) ShowPathTo(attackFrom.gridPosition);
            else MapOverlay.Instance.ClearPath();
            return;
        }

        bool onMap = inputController.tilemap.GetTile(hoveredCell) != null;
        if (onMap && ShowPathTo(hoveredCell)) return;

        MapOverlay.Instance.HideReticle();
        MapOverlay.Instance.ClearPath();
    }

    // Draws the path to `cell` if this unit can reach it. Returns whether it could.
    private bool ShowPathTo(Vector3Int cell)
    {
        Vector3Int unitCell = Mover.playerUnit.GridPosition;
        MovementClass movementClass = Mover.playerUnit.unitAttributes.movementClass;
        List<Node> path = PathfinderController.Instance.FindPath(unitCell, cell, movementClass);
        bool canReach = path != null && PathfinderController.Instance.GetPathCost(path, movementClass) <= Mover.playerUnit.unitAttributes.movement;

        if (canReach) MapOverlay.Instance.ShowPath(unitCell, path);
        else MapOverlay.Instance.ClearPath();
        return canReach;
    }

    public override void OnClick(Vector3Int cell)
    {
        TileBase tile = inputController.tilemap.GetTile(cell);

        List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(cell);

        Vector3Int playerGridPos = inputController.tilemap.WorldToCell(Mover.transform.position);
        
        if (playerGridPos == cell)
        {
            Mover.DeactivatePlayer();
            inputController.ReturnToIdle();
            return;
        }

        EnemyUnit clickedEnemy = objectsAtTile.OfType<EnemyUnit>().FirstOrDefault();
        if (clickedEnemy != null && !Mover.isMoving)
        {
            WeaponInstance weapon = (inputController.forecastWeapon != null && inputController.lastForecastTarget == clickedEnemy) ? inputController.forecastWeapon : null;
            Node bestTile = (inputController.forecastBestTile != null && inputController.lastForecastTarget == clickedEnemy) ? inputController.forecastBestTile : null;
            if (weapon == null || bestTile == null)
            {
                MovementClass mc2 = Mover.playerUnit.unitAttributes.movementClass;
                List<Node> reachable = PathfinderController.Instance.GetReachableNodes(playerGridPos, Mover.playerUnit.unitAttributes.movement, mc2);
                (weapon, bestTile) = inputController.FindBestAttackOption(Mover.playerUnit, clickedEnemy, reachable);
            }
            if (weapon != null && bestTile != null)
            {
                MovementClass mc = Mover.playerUnit.unitAttributes.movementClass;
                Mover.playerUnit.inventory.Equip(weapon);
                List<Node> path = PathfinderController.Instance.FindPath(playerGridPos, bestTile.gridPosition, mc);
                Mover.QueueAttack(clickedEnemy, weapon);
                inputController.ReturnToIdle();
                Mover.StartMoving(path, Mover.playerUnit.unitAttributes.movement);
            }
            return;
        }

        bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || obj is EnemyUnit);
        if (validTile && tile != null)
        {

            MovementClass movementClass = Mover.playerUnit.unitAttributes.movementClass;
            List<Node> path = PathfinderController.Instance.FindPath(playerGridPos, cell, movementClass);
            if (path != null && path.Count > 0 && PathfinderController.Instance.GetPathCost(path, movementClass) <= Mover.playerUnit.unitAttributes.movement)
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

    public override string ToString()
    {
        return $"{Mover.name} has been selected";
    }
}
