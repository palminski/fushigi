using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Unit : MapObject
{
    public Character character;
    protected SpriteRenderer spriteRenderer;
    protected Color baseColor;
    public UnitAttributes unitAttributes;
    public bool canAct = true;
    public Mover mover;
    public Inventory inventory;

    public bool hasPickedUpUnit = false;
    public bool hasDroppedUnit = false;
    public Unit heldUnit {get; private set;}

    public event System.Action HealthChanged;
    public event System.Action HeldUnitChanged;
    public event System.Action Died; // raised just before the unit is destroyed
    public event System.Action<Unit> PickedUp; // raised on the unit being picked up (rescued or captured), with who picked it up
    public event System.Action<Unit> Dropped;  // raised on a carried unit when it's set down, with who carried it
    public event System.Action<Unit> Released; // raised on a captive just before it's let go (and removed), with who held it

    public IEnumerable<WeaponInstance> UsableWeapons => inventory.items.OfType<WeaponInstance>().Where(w => CanUse(w.WeaponData));
    public WeaponInstance EquippedWeapon => UsableWeapons.FirstOrDefault();

    protected virtual void Awake()
    {
        if (character.baseLevel > unitAttributes.level) unitAttributes.level = character.baseLevel;
        if (character.bases.health > unitAttributes.health) unitAttributes.health = character.bases.health;
        if (character.bases.strength > unitAttributes.strength) unitAttributes.strength = character.bases.strength;
        if (character.bases.skill > unitAttributes.skill) unitAttributes.skill = character.bases.skill;
        if (character.bases.speed > unitAttributes.speed) unitAttributes.speed = character.bases.speed;
        if (character.bases.defence > unitAttributes.defence) unitAttributes.defence = character.bases.defence;
        if (character.bases.build > unitAttributes.build) unitAttributes.build = character.bases.build;
        if (character.bases.magic > unitAttributes.magic) unitAttributes.magic = character.bases.magic;
        if (character.bases.movement > unitAttributes.movement) unitAttributes.movement = character.bases.movement;

        unitAttributes.movement += character.characterClass.statModifiers.movement;


        unitAttributes.currentHealth = unitAttributes.health;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        mover = GetComponent<Mover>();
        inventory.Initialize();
    }

    public void TakeDamage(int damage)
    {
        unitAttributes.currentHealth -= damage;
        HealthChanged?.Invoke();

        if (unitAttributes.currentHealth <= 0)
        {
            Died?.Invoke();
            Destroy(gameObject);
        }
    }

    public void Heal(int healAmount)
    {
        unitAttributes.currentHealth = Mathf.Min(unitAttributes.currentHealth + healAmount,unitAttributes.health);
        HealthChanged?.Invoke();
    }

    public void PickUpUnit(Unit unit)
    {
        heldUnit = unit;
        hasPickedUpUnit = true;
        unit.gameObject.SetActive(false);
        // A capture "kills" without dealing damage, so the capture animation left this unit's bar drained to 0.
        // Redraw it at its real health for whenever it's set down again.
        unit.HealthChanged?.Invoke();
        unit.PickedUp?.Invoke(this);
        MapManager.Instance.RefreshMap();
        HeldUnitChanged?.Invoke();

    }

    public void TakeUnitFromAlly(PlayerUnit ally)
    {
        if(ally.heldUnit == null) return;
        heldUnit = ally.heldUnit;
        ally.BequeathUnit();
        MapManager.Instance.RefreshMap();
        HeldUnitChanged?.Invoke();

    }

    public void DropUnit(Vector3Int gridPosition)
    {
        Unit dropped = heldUnit;
        hasDroppedUnit = true;
        heldUnit = null;
        if(dropped is PlayerUnit droppedPlayer) droppedPlayer.SetInactive();
        dropped.gameObject.SetActive(true);
        dropped.transform.position = LevelMap.Instance.CellCenter(gridPosition);
        dropped.Dropped?.Invoke(this);
        MapManager.Instance.RefreshMap();
        HeldUnitChanged?.Invoke();

    }

    public void BequeathUnit()
    {
        heldUnit = null;
        HeldUnitChanged?.Invoke();

    }

    public void ReleaseHeld()
    {
        heldUnit.Released?.Invoke(this);
        Destroy(heldUnit.gameObject);
        heldUnit = null;
        HeldUnitChanged?.Invoke();

    }

    public IEnumerator CaptureCoroutine(Unit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.ResolveCapture(this, target, weapon);
        yield return CombatPresenter.Instance.Present(this,target,result);
        WeaponInstance defWeapon = (result.defenderSwings > 0 && target != null) ? target.EquippedWeapon : null;

        if (result.defenderDied && target != null)
        {
            PickUpUnit(target);
        }
        else if (result.totalDamageToDefender > 0 && target!=null)
        {
            target.TakeDamage(result.totalDamageToDefender);
        }
        for (int i = 0; i < result.defenderSwings; i++) defWeapon?.Use();
        if(result.totalDamageToAttacker > 0) TakeDamage(result.totalDamageToAttacker);
        for (int i = 0; i < result.attackerSwings; i++) weapon.Use();
        yield return AfterCombat(this, target, result);
    }

    protected IEnumerator AfterCombat(Unit attacker, Unit defender, CombatResult result)
    {
        PlayerUnit playerUnit = null;
        if (attacker is PlayerUnit attackingPlayer)
        {
            playerUnit = attackingPlayer;
        }
        else if (defender is PlayerUnit defendingPlayer)
        {
            playerUnit = defendingPlayer;
        }

        bool playerDied = playerUnit == attacker ? result.attackerDied : result.defenderDied;
        //EXP
        if (playerUnit != null && !playerDied)
        {
            int startingExp = playerUnit.unitAttributes.currentExp;
            int expToAdd = 0;
            //Player Defending
            if (result.attackerDied && attacker != playerUnit)
            {
                expToAdd = CombatCalculator.CalculateExp(playerUnit, attacker, true);
            }
            //Player Attacking
            else if (result.defenderDied && defender != playerUnit)
            {
                expToAdd = CombatCalculator.CalculateExp(playerUnit, defender, true);
            }
            //Enemy not killed
            else if (result.totalDamageToAttacker > 0 || result.totalDamageToDefender > 0)
            {
                if(defender != playerUnit)
                {
                    expToAdd = CombatCalculator.CalculateExp(playerUnit, defender, false);
                }
                else
                {
                    expToAdd = CombatCalculator.CalculateExp(playerUnit, attacker, false);
                    
                };
            }
            if(expToAdd > 0)
            {
                yield return CombatPresenter.Instance.PresentExp(playerUnit, startingExp, expToAdd);
                StatBlock gainedStats = playerUnit.GetExp(expToAdd);
                if(gainedStats != null)
                {
                    yield return CombatPresenter.Instance.PresentLevelUp(playerUnit, gainedStats);
                }
            }
        }
    }

    

    public bool CanUse(Weapon weapon)
    {
        if(weapon == null) return false;
        if(character == null)
        {
            return true;
        }

        JobClass jobClass = character.characterClass;
        if(jobClass != null && !jobClass.usableWeapons.Contains(weapon.weaponType)) return false;
        List<Skill> unitSkills = Skills();
        foreach (Skill requiredSkill in weapon.requiredSkills)
        {
            if (!unitSkills.Contains(requiredSkill)) return false;
        }
        return true;
    }

    public List<Skill> Skills()
    {
        List<Skill> allSkills = new();
        if(character == null) return allSkills;
        allSkills.AddRange(character.skills);
        if(character.characterClass != null) allSkills.AddRange(character.characterClass.skills);
        return allSkills;
    }
}
