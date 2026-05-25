using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitAttributes
{
    public int currentHealth;
    [Space(10)]
    public int health = 1;
    public int strength = 1;
    public int speed = 1;
    public int defence = 1;
    public int magic = 1;
    public int movement = 8;
    public MovementClass movementClass = MovementClass.Infantry;

}
