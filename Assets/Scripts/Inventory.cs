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

    public bool IsFull => items.Count >= maxCapacity;

    public static ItemInstance CreateInstance(Item item)
    {
        if (item is Weapon weapon) return new WeaponInstance { data = weapon, currentDurability = weapon.maxDurability };
        if (item is Consumable) return new ConsumableInstance { data = item };
        return new ItemInstance {data = item};
    }

    public bool AddItem(Item item)
    {
        if(item == null) return false;
        if (IsFull)
        {
            GameSaveManager.SendToConvoy(item);
            return false;
        }
        items.Add(CreateInstance(item));
        return true;
    }

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
            items.Add(CreateInstance(item));
        }
    }
}
