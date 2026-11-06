using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Works out where a unit can attack a target from, and with which weapon.
// Pure calculation: it reads the map and combat previews but never changes anything.
public static class AttackPlanner
{
    // Grid distance (no diagonals), the same distance weapon ranges use
    public static int Distance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Weapons in the unit's inventory that can hit something `distance` tiles away
    public static List<WeaponInstance> WeaponsInRange(Unit unit, int distance)
    {
        return unit.inventory.items.OfType<WeaponInstance>().Where(w => w.CanHitAt(distance)).ToList();
    }

    // For each weapon that can reach the target, the best tile to attack from
    public static List<(WeaponInstance weapon, Node tile)> AttackOptions(Unit attacker, Unit target, List<Node> reachable)
    {
        var options = new List<(WeaponInstance, Node)>();
        foreach (WeaponInstance weapon in attacker.inventory.items.OfType<WeaponInstance>())
        {
            var (tile, _) = BestTileForWeapon(attacker, target, weapon, reachable);
            if (tile != null) options.Add((weapon, tile));
        }
        return options;
    }

    // The best weapon and tile overall, or (null, null) if the target can't be reached
    public static (WeaponInstance weapon, Node tile) BestAttack(Unit attacker, Unit target, List<Node> reachable)
    {
        int bestScore = int.MinValue;
        WeaponInstance bestWeapon = null;
        Node bestTile = null;
        foreach (WeaponInstance weapon in attacker.inventory.items.OfType<WeaponInstance>())
        {
            var (tile, score) = BestTileForWeapon(attacker, target, weapon, reachable);
            if (tile != null && score > bestScore) { bestScore = score; bestWeapon = weapon; bestTile = tile; }
        }
        return (bestWeapon, bestTile);
    }

    private static (Node tile, int score) BestTileForWeapon(Unit attacker, Unit target, WeaponInstance weapon, List<Node> reachable)
    {
        int bestScore = int.MinValue;
        Node bestTile = null;
        foreach (Node node in reachable)
        {
            // Can't attack from a tile another unit is standing on
            if (MapManager.Instance.GetObjectsAt(node.gridPosition).Any(o => o is Unit u && u != attacker)) continue;
            if (!weapon.CanHitAt(Distance(node.gridPosition, target.GridPosition))) continue;

            int score = Score(CombatCalculator.Preview(attacker, target, weapon, node.gridPosition));
            if (score > bestScore) { bestScore = score; bestTile = node; }
        }
        return (bestTile, bestScore);
    }

    // How good an attack is for the player: avoiding a counter matters most, then killing, then damage
    private static int Score(CombatPreview preview)
    {
        int score = preview.damageDealt;
        if (preview.killsDefender) score += 100;
        if (!preview.defenderCanCounter) score += 1000;
        return score;
    }
}
