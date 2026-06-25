using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyUnit : Unit
{

    private Unit plannedTarget;
    private WeaponInstance plannedWeapon;
    // Update is called once per frame
    void Update()
    {
        
    }

    public void PerformTurnMovement()
    {
        MapManager.Instance.RefreshMap();
        Node targetNode = FindTargetTile();

        if (targetNode != null)
        {
            List<Node> path = PathfinderController.Instance.FindPath(mover.transform.position, targetNode.gridPosition, unitAttributes.movementClass);
            mover.StartMoving(path);
        }
    }

    public IEnumerator PerformTurnActionCoroutine()
    {
        if (plannedTarget == null || plannedWeapon == null) yield break;
        yield return StartCoroutine(AttackCoroutine(plannedTarget, plannedWeapon));
    }

    private Node FindTargetTile()
    {
        List<Node> reachableNodes = PathfinderController.Instance.GetReachableNodes(mover.transform.position, unitAttributes.movement, unitAttributes.movementClass);
        var playerUnits = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);

        int bestScore = int.MinValue;
        Node targetNode = null;

        foreach (Node reachableNode in reachableNodes)
        {
            List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(reachableNode.gridPosition);
            bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || (obj is EnemyUnit enemyUnit && enemyUnit != this));
            if (!validTile) continue;

            foreach (PlayerUnit playerUnit in playerUnits)
            {
                Vector3Int nodePos = reachableNode.gridPosition;
                Vector3Int playerPos = playerUnit.GridPosition;
                int distance = Mathf.Abs(nodePos.x - playerPos.x) + Mathf.Abs(nodePos.y - playerPos.y);

                foreach (ItemInstance item in inventory.items)
                {
                    if (item is not WeaponInstance weapon) continue;
                    if (distance < weapon.minRange || distance > weapon.maxRange) continue;

                    CombatPreview preview = CombatCalculator.Preview(this, playerUnit, weapon, reachableNode.gridPosition);

                    int score = preview.damageDealt;
                    if (preview.killsDefender) score += 1000;
                    if (!preview.defenderCanCounter) score += 100;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        targetNode = reachableNode;
                        plannedTarget = playerUnit;
                        plannedWeapon = weapon;
                    }
                }
            }
        }
        if (targetNode == null)
        {
            int closestDistance = int.MaxValue;
            foreach (Node reachableNode in reachableNodes)
            {
                List<MapObject> objectsAtTile = MapManager.Instance.GetObjectsAt(reachableNode.gridPosition);
                bool validTile = !objectsAtTile.Any(obj => obj is PlayerUnit || (obj is EnemyUnit enemyUnit && enemyUnit != this));
                if (!validTile) continue;

                foreach (PlayerUnit playerUnit in playerUnits)
                {
                    Vector3Int nodePos = reachableNode.gridPosition;
                    Vector3Int playerPos = playerUnit.GridPosition;
                    int distance = Mathf.Abs(nodePos.x - playerPos.x) + Mathf.Abs(nodePos.y - playerPos.y);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        targetNode = reachableNode;
                    }
                }
            }
        }

        return targetNode;
    }

    public IEnumerator AttackCoroutine(Unit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.Resolve(this, target, weapon);
        CombatScreenController.Instance.Show(result);
        yield return new WaitUntil(() => !ScreenManager.Instance.IsBlocking);
        WeaponInstance defWeapon = (result.defenderSwings > 0 && target != null) ? target.inventory.EquippedWeapon : null;
        if (result.totalDamageToDefender > 0 && target != null)
            target.TakeDamage(result.totalDamageToDefender);
        for (int i = 0; i < result.defenderSwings; i++) defWeapon?.Use();
        if (result.totalDamageToAttacker > 0) TakeDamage(result.totalDamageToAttacker);
        for (int i = 0; i < result.attackerSwings; i++) weapon.Use();
    }
}
