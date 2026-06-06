using System.Collections.Generic;
using UnityEngine;

public struct CombatPreview
{
    public int damageDealt;
    public int damageReceived;
    public bool killsDefender;
    public bool defenderCanCounter;
    public bool attackerDoubles;
    public bool defenderDoubles;

    public CombatPreview(int damageDealt, int damageReceived, bool killsDefender, bool defenderCanCounter, bool attackerDoubles = false, bool defenderDoubles = false)
    {
        this.damageDealt = damageDealt; this.damageReceived = damageReceived;
        this.killsDefender = killsDefender; this.defenderCanCounter = defenderCanCounter;
        this.attackerDoubles = attackerDoubles; this.defenderDoubles = defenderDoubles;
    }
}

public struct CombatEvent
{
    public string hitterName;
    public string hitName;
    public int damage;
    public int hitHpBefore;
    public int hitHpAfter;
    public bool wasFatal => hitHpAfter <= 0;
    public bool hitWasPlayer;

    public CombatEvent(string hitter, string hit, int dmg, int hpBefore, int hpAfter, bool wasPlayer=false)
    {
        hitterName = hitter; hitName = hit; damage = dmg;
        hitHpBefore = hpBefore; hitHpAfter = hpAfter;
        hitWasPlayer = wasPlayer;
    }
}

public struct CombatResult
{
    public List<CombatEvent> events;
    public int totalDamageToAttacker;
    public int totalDamageToDefender;
    public int attackerSwings;
    public int defenderSwings;
    public bool attackerDied;
    public bool defenderDied;
}

public static class CombatCalculator
{
    private static int CalcDamage(Unit atk, Unit def, WeaponInstance w)
        => Mathf.Max(0, atk.unitAttributes.strength + w.might - def.unitAttributes.defence);

    public static CombatPreview Preview(Unit attacker, Unit defender, WeaponInstance weapon, Vector3Int? fromPosition = null)
    {
        Vector3Int attackerPos = fromPosition ?? attacker.GridPosition;
        int distance = Mathf.Abs(attackerPos.x - defender.GridPosition.x) + Mathf.Abs(attackerPos.y - defender.GridPosition.y);

        int damageDealt = CalcDamage(attacker, defender, weapon);
        WeaponInstance defWeapon = defender.inventory.EquippedWeapon;
        bool defenderCanCounter = defWeapon != null && distance >= defWeapon.minRange && distance <= defWeapon.maxRange;
        int damageReceived = defenderCanCounter ? CalcDamage(defender, attacker, defWeapon) : 0;
        bool killsDefender = damageDealt >= defender.unitAttributes.currentHealth;
        bool attackerDoubles = attacker.unitAttributes.speed >= defender.unitAttributes.speed + 5;
        bool defenderDoubles = defender.unitAttributes.speed >= attacker.unitAttributes.speed + 5;

        return new CombatPreview(damageDealt, damageReceived, killsDefender, defenderCanCounter, attackerDoubles, defenderDoubles);
    }

    public static CombatResult Resolve(Unit attacker, Unit defender, WeaponInstance weapon, Vector3Int? fromPosition = null)
    {
        Vector3Int attackerPos = fromPosition ?? attacker.GridPosition;
        int distance = Mathf.Abs(attackerPos.x - defender.GridPosition.x) + Mathf.Abs(attackerPos.y - defender.GridPosition.y);

        int atkDmg = CalcDamage(attacker, defender, weapon);
        WeaponInstance defWeapon = defender.inventory.EquippedWeapon;
        bool canCounter = defWeapon != null && distance >= defWeapon.minRange && distance <= defWeapon.maxRange;
        int defDmg = canCounter ? CalcDamage(defender, attacker, defWeapon) : 0;
        bool attackerDoubles = attacker.unitAttributes.speed >= defender.unitAttributes.speed + 5;
        bool defenderDoubles = defender.unitAttributes.speed >= attacker.unitAttributes.speed + 5;

        var events = new List<CombatEvent>();
        int atkHp = attacker.unitAttributes.currentHealth;
        int defHp = defender.unitAttributes.currentHealth;
        int attackerSwings = 0, defenderSwings = 0;
        string atkName = attacker.gameObject.name, defName = defender.gameObject.name;
        bool atkWasPlayer = attacker is PlayerUnit ? true : false;

        // Hit 1 — attacker
        { int b = defHp; defHp = Mathf.Max(0, defHp - atkDmg); attackerSwings++;
          events.Add(new CombatEvent(atkName, defName, atkDmg, b, defHp, atkWasPlayer));
          if (defHp <= 0) goto done; }

        // Hit 2 — defender counter
        if (canCounter)
        { int b = atkHp; atkHp = Mathf.Max(0, atkHp - defDmg); defenderSwings++;
          events.Add(new CombatEvent(defName, atkName, defDmg, b, atkHp, !atkWasPlayer));
          if (atkHp <= 0) goto done; }

        // Hit 3 — attacker doubles
        if (attackerDoubles)
        { int b = defHp; defHp = Mathf.Max(0, defHp - atkDmg); attackerSwings++;
          events.Add(new CombatEvent(atkName, defName, atkDmg, b, defHp, atkWasPlayer));
          if (defHp <= 0) goto done; }

        // Hit 4 — defender doubles
        if (defenderDoubles && canCounter)
        { int b = atkHp; atkHp = Mathf.Max(0, atkHp - defDmg); defenderSwings++;
          events.Add(new CombatEvent(defName, atkName, defDmg, b, atkHp, !atkWasPlayer)); }

        done:
        return new CombatResult
        {
            events = events,
            totalDamageToAttacker = attacker.unitAttributes.currentHealth - atkHp,
            totalDamageToDefender = defender.unitAttributes.currentHealth - defHp,
            attackerSwings = attackerSwings,
            defenderSwings = defenderSwings,
            attackerDied = atkHp <= 0,
            defenderDied = defHp <= 0,
        };
    }
}
