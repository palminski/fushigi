using UnityEngine;

[System.Serializable]
public class ConsumableInstance : ItemInstance
{
    public Consumable ConsumableData => (Consumable)data;

    public void Use(Unit unit)
    {
        unit.Heal(ConsumableData.healAmount);
    }
}
