using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class Weapon : Item
{
    public int minRange;
    public int maxRange;
    public int might;
    public int weight;
    public WeaponType weaponType;
}
