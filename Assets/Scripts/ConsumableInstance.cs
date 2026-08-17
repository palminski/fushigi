using UnityEngine;

[System.Serializable]
public class ConsumableInstance : ItemInstance
{
    public Consumable ConsumableData => (Consumable)data;

    public void Use(Unit unit)
    {
        unit.unitAttributes.currentHealth = Mathf.Min(
            unit.unitAttributes.currentHealth + ConsumableData.healAmount,
            unit.unitAttributes.health
        );
    }
}
