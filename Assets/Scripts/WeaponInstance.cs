using UnityEngine;

[System.Serializable]
public class WeaponInstance : ItemInstance
{
    public int currentDurability;

    public Weapon WeaponData => (Weapon)data;
    public int minRange => WeaponData.minRange;
    public int maxRange => WeaponData.maxRange;
    public int might => IsBroken ? 1 : WeaponData.might;
    public int weight => WeaponData.weight;
    public WeaponType weaponType => WeaponData.weaponType;
    public bool CanHitAt(int dist) => dist >= minRange && dist <= maxRange;
    public bool IsBroken => currentDurability <= 0;

    public void Use()
    {
        if (currentDurability > 0) currentDurability--;
    }
}
