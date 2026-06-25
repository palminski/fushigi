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

    public IEnumerator AttackCoroutine(EnemyUnit target, WeaponInstance weapon)
    {
        if (weapon == null) yield break;
        CombatResult result = CombatCalculator.Resolve(this, target, weapon);
        CombatScreenController.Instance.Show(result);
        yield return new WaitUntil(() => !ScreenManager.Instance.IsBlocking);
        WeaponInstance defWeapon = (result.defenderSwings > 0 && target != null) ? target.inventory.EquippedWeapon : null;
        if (result.totalDamageToDefender > 0 && target != null)
            target.TakeDamage(result.totalDamageToDefender);
        for (int i = 0; i < result.defenderSwings; i++) defWeapon?.Use();
        if (result.totalDamageToAttacker > 0) TakeDamage(result.totalDamageToAttacker);
        for (int i = 0; i < result.attackerSwings; i++) weapon.Use();
    }
}
