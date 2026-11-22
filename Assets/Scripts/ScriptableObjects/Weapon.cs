using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Scriptable Objects/Weapon")]
public class Weapon : Item
{
    public int minRange;
    public int maxRange;
    public int might;
    public int hit = 80;
    public int weight;
    public int maxDurability = 10;
    public WeaponType weaponType;
    public List<Skill> requiredSkills;
    public bool CanHitAt(int dist) => dist >= minRange && dist <= maxRange;
}
