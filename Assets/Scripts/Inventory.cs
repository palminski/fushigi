using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class Inventory
{
    public List<Item> startingItems = new List<Item>();

    [UnityEngine.SerializeReference]
    public List<ItemInstance> items = new List<ItemInstance>();

    public int maxCapacity = 5;
    public WeaponInstance EquippedWeapon => items.OfType<WeaponInstance>().FirstOrDefault();

    public void Equip(WeaponInstance weapon)
    {
        int index = items.IndexOf(weapon);
        if (index <= 0) return;
        items.RemoveAt(index);
        items.Insert(0, weapon);
    }

    public void Initialize()
    {
        items.Clear();
        foreach (Item item in startingItems)
        {
            if (item is Weapon weapon)
                items.Add(new WeaponInstance { data = weapon, currentDurability = weapon.maxDurability });
            else if (item is Consumable)
                items.Add(new ConsumableInstance { data = item });
            else
                items.Add(new ItemInstance { data = item });
        }
    }
}
