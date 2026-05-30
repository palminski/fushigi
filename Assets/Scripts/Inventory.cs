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

    public void Initialize()
    {
        items.Clear();
        foreach (Item item in startingItems)
        {
            if (item is Weapon weapon)
                items.Add(new WeaponInstance { data = weapon, currentDurability = weapon.maxDurability });
            else
                items.Add(new ItemInstance { data = item });
        }
    }
}
