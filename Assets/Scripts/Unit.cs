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

    [SerializeField] private Transform healthBar;
    [SerializeField] private SpriteRenderer holdingUnitIndicator;
    [SerializeField] private SpriteRenderer toggledOnEnemyIndicator;

    protected virtual void Awake()
    {
        unitAttributes.currentHealth = unitAttributes.health;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        mover = GetComponent<Mover>();
        inventory.Initialize();
        UpdateIndicators();
    }

    public void TakeDamage(int damage)
    {
        unitAttributes.currentHealth -= damage;
        UpdateIndicators();
        if (unitAttributes.currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Heal(int healAmount)
    {
        unitAttributes.currentHealth = Mathf.Min(unitAttributes.currentHealth + healAmount,unitAttributes.health);
        UpdateIndicators();
    }

    public void UpdateIndicators()
    {
        if (healthBar != null && unitAttributes.health != 0)
        {
            float percentage = (float)unitAttributes.currentHealth / unitAttributes.health;
            healthBar.localScale = new(percentage, healthBar.localScale.y, healthBar.localScale.z);
        }
        if (holdingUnitIndicator != null)
        {
            if(heldUnit != null)
            {
                holdingUnitIndicator.gameObject.SetActive(true);
                holdingUnitIndicator.color = (heldUnit is PlayerUnit) ? Color.blue : Color.red;
            }
            else
            {
                holdingUnitIndicator.gameObject.SetActive(false);
            }
        }
        if (toggledOnEnemyIndicator != null)
        {
            if(this is EnemyUnit thisEnemy && InputController.Instance.toggledEnemies.Contains(thisEnemy))
            {
                toggledOnEnemyIndicator.gameObject.SetActive(true);
            }
            else
            {
                toggledOnEnemyIndicator.gameObject.SetActive(false);
            }
            
        }
    }

    public void PickUpUnit(Unit unit)
    {
        heldUnit = unit;
        hasPickedUpUnit = true;
        unit.gameObject.SetActive(false);
        MapManager.Instance.RefreshMap();
        UpdateIndicators();

    }

    public void TakeUnitFromAlly(PlayerUnit ally)
    {
        if(ally.heldUnit == null) return;
        heldUnit = ally.heldUnit;
        ally.BequeathUnit();
        MapManager.Instance.RefreshMap();
        UpdateIndicators();

    }

    public void DropUnit(Vector3Int gridPosition)
    {
        Unit dropped = heldUnit;
        hasDroppedUnit = true;
        heldUnit = null;
        if(dropped is PlayerUnit droppedPlayer) droppedPlayer.SetInactive();
        dropped.gameObject.SetActive(true);
        dropped.transform.position = mover.tilemap.GetCellCenterWorld(gridPosition);
        MapManager.Instance.RefreshMap();
        UpdateIndicators();

    }

    public void BequeathUnit()
    {
        heldUnit = null;
        UpdateIndicators();

    }

    public void ReleaseHeld()
    {
        Destroy(heldUnit.gameObject);
        heldUnit = null;
        UpdateIndicators();

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
