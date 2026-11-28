using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerUnit : Unit
{
    // Topics only this unit can ask NPCs about (on top of the ones everyone remembers). Moves to a unit definition later.
    public List<Topic> personalTopics = new List<Topic>();

    // Update is called once per frame
    void Update()
    {

    }

    public void SetInactive()
    {
        canAct = false;
        float lumunence = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
        Color inactiveColor = new Color(lumunence, lumunence, lumunence, 0.5f);
        spriteRenderer.color = inactiveColor;
        GamePhaseManager.Instance.CheckAndChangePhase(); //Temporary for now
    }

    public void SetActive()
    {
        canAct = true;
        hasPickedUpUnit = false;
        hasDroppedUnit = false;
        spriteRenderer.color = baseColor;
    }

    // Attack, then end this unit's turn
    public IEnumerator AttackAndEndTurn(EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon != null) yield return AttackCoroutine(target, weapon);
        if (this != null) SetInactive(); // this unit may have died to the counter-attack
    }

    // Capture attempt, then end this unit's turn
    public IEnumerator CaptureAndEndTurn(EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon != null) yield return CaptureCoroutine(target, weapon);
        if (this != null) SetInactive();
    }

    public IEnumerator AttackCoroutine(EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.Resolve(this, target, weapon);
        yield return CombatPresenter.Instance.Present(this,target,result);
        WeaponInstance defWeapon = (result.defenderSwings > 0 && target != null) ? target.EquippedWeapon : null;
        if (result.totalDamageToDefender > 0 && target != null)
            target.TakeDamage(result.totalDamageToDefender);
        for (int i = 0; i < result.defenderSwings; i++) defWeapon?.Use();
        if (result.totalDamageToAttacker > 0) TakeDamage(result.totalDamageToAttacker);
        for (int i = 0; i < result.attackerSwings; i++) weapon.Use();
        yield return AfterCombat(this, target, result);
    }

    public StatBlock GetExp(int exp)
    {
        StatBlock gainedStats = null;
        int newExpValue = unitAttributes.currentExp + exp;

        if (newExpValue >= 100)
        {
            newExpValue = Mathf.Min(newExpValue - 100, 99);
            gainedStats = LevelUp();
        }
        unitAttributes.currentExp = newExpValue;
        return gainedStats;
    }

    private StatBlock LevelUp()
    {
        StatBlock growths = character.growths;
        StatBlock gains = new();

        gains.health += RollForGrowth(growths.health) ? 1 : 0;
        gains.strength += RollForGrowth(growths.strength) ? 1 : 0;
        gains.skill += RollForGrowth(growths.skill) ? 1 : 0;
        gains.speed += RollForGrowth(growths.speed) ? 1 : 0;
        gains.defence += RollForGrowth(growths.defence) ? 1 : 0;
        gains.build += RollForGrowth(growths.build) ? 1 : 0;
        gains.magic += RollForGrowth(growths.magic) ? 1 : 0;
        gains.movement += RollForGrowth(growths.movement) ? 1 : 0;

        unitAttributes.level ++;
        unitAttributes.health += gains.health;
        Heal(gains.health);
        unitAttributes.strength += gains.strength;
        unitAttributes.skill += gains.skill;
        unitAttributes.speed += gains.speed;
        unitAttributes.defence += gains.defence;
        unitAttributes.build += gains.build;
        unitAttributes.magic += gains.magic;
        unitAttributes.movement += gains.movement;

        return gains;
    }   

    private bool RollForGrowth(int growth)
    {
        int roll = Random.Range(0,100); //max 99
        if (roll < growth) return true;
        return false;
    }
}
