using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitAttributes : MonoBehaviour
{
    public int currentHealth;
[Space(10)]
    public int health = 1;
    public int strength = 1;
    public int speed = 1;
    public int defence = 1;
    public int magic = 1;
    public int movement = 1;



    // Start is called before the first frame update
    void Start()
    {
        currentHealth = health;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Damage(int damage)
    {
        currentHealth -= damage;
        print("Unit Health = " + currentHealth);
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }
}
