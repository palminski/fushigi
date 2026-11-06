using System.Collections;
using UnityEngine;

public abstract class Unit : MapObject
{
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

    protected virtual void Awake()
    {
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
        Destroy(heldUnit.gameObject);
        heldUnit = null;
        HeldUnitChanged?.Invoke();

    }

    public IEnumerator CaptureCoroutine(Unit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.ResolveCapture(this, target, weapon);
        CombatScreen.Instance.Show(result);
        yield return new WaitUntil(() => !ScreenManager.Instance.IsBlocking);
        WeaponInstance defWeapon = (result.defenderSwings > 0 && target != null) ? target.inventory.EquippedWeapon : null;

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
    }
}
