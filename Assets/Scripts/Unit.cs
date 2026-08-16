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

    public Unit heldUnit {get; private set;}

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
        if (unitAttributes.currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void PickUpUnit(Unit unit)
    {
        heldUnit = unit;
        unit.gameObject.SetActive(false);
        MapManager.Instance.RefreshMap();
    }

    public void dropUnit(Vector3Int gridPosition)
    {
        Unit dropped = heldUnit;
        heldUnit = null;
        dropped.gameObject.SetActive(true);
        dropped.transform.position = mover.tilemap.GetCellCenterWorld(gridPosition);
        MapManager.Instance.RefreshMap();
    }

    public void ReleaseHeld()
    {
        Destroy(heldUnit.gameObject);
        heldUnit = null;
    }

    public IEnumerator CaptureCoroutine(Unit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.ResolveCapture(this, target, weapon);
        CombatScreenController.Instance.Show(result);
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
