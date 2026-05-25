using System.Collections.Generic;
using System.Linq;
[System.Serializable]
public class Inventory
{
    public List<Item> items = new List<Item>();
    public int maxCapacity = 5;
    public Weapon EquippedWeapon => items.OfType<Weapon>().FirstOrDefault();
}
