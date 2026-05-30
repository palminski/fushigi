using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerUnit : Unit
{
    // Update is called once per frame
    void Update()
    {

    }

    public void SetInactive()
    {
        canAct = false;
        float lumunence = 0.299f * baseColor.r + 0.587f * baseColor.g + 0.114f * baseColor.b;
        Color inactiveColor = new Color(lumunence, lumunence, lumunence, 0.5f);
        spriteRenderer.color = inactiveColor;
        GameController.Instance.CheckAndChangePhase(); //Temporary for now
    }

    public void SetActive()
    {
        canAct = true;
        spriteRenderer.color = baseColor;
    }

    public void Attack(EnemyUnit target, Weapon weapon)
    {
        
        if (weapon == null) return;
        CombatPreview preview  = CombatCalculator.Preview(this, target, weapon);
        target.TakeDamage(preview.damageDealt);
        if(preview.defenderCanCounter)
        {
            TakeDamage(preview.damageReceived);
        }
    }
}
