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
    public int attackerHitChance;
    public int defenderHitChance;

    public CombatPreview(int damageDealt, int damageReceived, bool killsDefender, bool defenderCanCounter, bool attackerDoubles = false, bool defenderDoubles = false, int attackerHitChance = 100, int defenderHitChance = 0)
    {
        this.damageDealt = damageDealt; this.damageReceived = damageReceived;
        this.killsDefender = killsDefender; this.defenderCanCounter = defenderCanCounter;
        this.attackerDoubles = attackerDoubles; this.defenderDoubles = defenderDoubles;
        this.attackerHitChance = attackerHitChance; this.defenderHitChance = defenderHitChance;
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

    public bool didHit;

    public CombatEvent(string hitter, string hit, int dmg, int hpBefore, int hpAfter, bool wasPlayer = false, bool didHit = true)
    {
        hitterName = hitter; hitName = hit; damage = dmg;
        hitHpBefore = hpBefore; hitHpAfter = hpAfter;
        hitWasPlayer = wasPlayer; this.didHit = didHit;
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

    private static int CalcAvoid(Unit unit, WeaponInstance equippedWeapon, int terrainAvoid)
    {
        int weightPenalty = Mathf.Max(0, (equippedWeapon?.weight ?? 0) - unit.unitAttributes.build);
        return unit.unitAttributes.speed * 2 - weightPenalty + terrainAvoid;
    }

    private static int CalcHitChance(WeaponInstance weapon, Unit attacker, int defenderAvoid)
        => Mathf.Clamp(weapon.hit + attacker.unitAttributes.skill - defenderAvoid, 0, 100);

    private static bool RollHit(int hitPercent)
    {
        if (hitPercent <= 0) return false;
        if (hitPercent >= 100) return true;
        return Random.Range(0, 100) < hitPercent;
    }

    private static int GetTerrainAvoid(Vector3Int gridPos)
        => PathfinderController.Instance.GetNode(gridPos)?.terrainType?.avoidBonus ?? 0;

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

        int defenderAvoid = CalcAvoid(defender, defWeapon, GetTerrainAvoid(defender.GridPosition));
        int attackerAvoid = CalcAvoid(attacker, weapon, GetTerrainAvoid(attackerPos));
        int attackerHitChance = CalcHitChance(weapon, attacker, defenderAvoid);
        int defenderHitChance = defenderCanCounter ? CalcHitChance(defWeapon, defender, attackerAvoid) : 0;

        return new CombatPreview(damageDealt, damageReceived, killsDefender, defenderCanCounter, attackerDoubles, defenderDoubles, attackerHitChance, defenderHitChance);
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

        int defenderAvoid = CalcAvoid(defender, defWeapon, GetTerrainAvoid(defender.GridPosition));
        int attackerAvoid = CalcAvoid(attacker, weapon, GetTerrainAvoid(attackerPos));
        int atkHitChance = CalcHitChance(weapon, attacker, defenderAvoid);
        int defHitChance = canCounter ? CalcHitChance(defWeapon, defender, attackerAvoid) : 0;

        var events = new List<CombatEvent>();
        int atkHp = attacker.unitAttributes.currentHealth;
        int defHp = defender.unitAttributes.currentHealth;
        int attackerSwings = 0, defenderSwings = 0;
        string atkName = attacker.gameObject.name, defName = defender.gameObject.name;
        bool atkWasPlayer = attacker is PlayerUnit;

        // Hit 1 — attacker
        { bool hit = RollHit(atkHitChance); int b = defHp;
          if (hit) defHp = Mathf.Max(0, defHp - atkDmg); attackerSwings++;
          events.Add(new CombatEvent(atkName, defName, hit ? atkDmg : 0, b, defHp, atkWasPlayer, hit));
          if (defHp <= 0) goto done; }

        // Hit 2 — defender counter
        if (canCounter)
        { bool hit = RollHit(defHitChance); int b = atkHp;
          if (hit) atkHp = Mathf.Max(0, atkHp - defDmg); defenderSwings++;
          events.Add(new CombatEvent(defName, atkName, hit ? defDmg : 0, b, atkHp, !atkWasPlayer, hit));
          if (atkHp <= 0) goto done; }

        // Hit 3 — attacker doubles
        if (attackerDoubles)
        { bool hit = RollHit(atkHitChance); int b = defHp;
          if (hit) defHp = Mathf.Max(0, defHp - atkDmg); attackerSwings++;
          events.Add(new CombatEvent(atkName, defName, hit ? atkDmg : 0, b, defHp, atkWasPlayer, hit));
          if (defHp <= 0) goto done; }

        // Hit 4 — defender doubles
        if (defenderDoubles && canCounter)
        { bool hit = RollHit(defHitChance); int b = atkHp;
          if (hit) atkHp = Mathf.Max(0, atkHp - defDmg); defenderSwings++;
          events.Add(new CombatEvent(defName, atkName, hit ? defDmg : 0, b, atkHp, !atkWasPlayer, hit)); }

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
