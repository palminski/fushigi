using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatBlock
{
    public int health = 0;
    public int strength = 0;
    public int skill = 0;
    public int speed = 0;
    public int defence = 0;
    public int build = 0;
    public int magic = 0;
    public int movement = 0;

}

[CreateAssetMenu(fileName = "NewJobClass", menuName = "Scriptable Objects/Units/JobClass")]
public class JobClass : ScriptableObject
{
    public StatBlock statModifiers;

    public MovementClass movementClass;

    public List<JobClass> promotesTo;

    public List<Skill> skills;

    public List<WeaponType> usableWeapons;
}
