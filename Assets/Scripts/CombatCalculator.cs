using UnityEngine;

public struct CombatPreview
{
    public int damageDealt;
    public int damageReceived;
    public bool killsDefender;
    public bool defenderCanCounter;

    public CombatPreview(int damageDealt, int damageReceived, bool killsDefender, bool defenderCanCounter)
    {
        this.damageDealt = damageDealt;
        this.damageReceived = damageReceived;
        this.killsDefender = killsDefender;
        this.defenderCanCounter = defenderCanCounter;
    }
}

public static class CombatCalculator
{
    public static CombatPreview Preview(
        Unit attacker,
        Unit defender,
        WeaponInstance weapon,
        Vector3Int? fromPosition = null
    )
    {
        Vector3Int attackerPos = fromPosition ?? attacker.GridPosition;
        Vector3Int defenderPos = defender.GridPosition;

        int distance = Mathf.Abs(attackerPos.x - defenderPos.x) + Mathf.Abs(attackerPos.y - defenderPos.y);

        int damageDealt = Mathf.Max(0, attacker.unitAttributes.strength + weapon.might - defender.unitAttributes.defence);

        WeaponInstance defenderWeapon = defender.inventory.EquippedWeapon;
        bool defenderCanCounter = defenderWeapon != null
        && distance >= defenderWeapon.minRange
        && distance <= defenderWeapon.maxRange;

        int damageReceived = !defenderCanCounter ? 0 : Mathf.Max(0, defenderWeapon.might + defender.unitAttributes.strength - attacker.unitAttributes.defence);

        bool killsDefender = damageDealt >= defender.unitAttributes.currentHealth;

        return new CombatPreview(damageDealt, damageReceived, killsDefender, defenderCanCounter);

    }
}
