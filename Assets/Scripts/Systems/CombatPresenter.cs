using System.Collections;
using UnityEngine;

public class CombatPresenter : MonoBehaviour
{
    public static CombatPresenter Instance {get; private set;}
    public enum CombatAnimationMode
    {
        Full,
        Map,
        None,
    }

    [SerializeField]private CombatReadoutScreen readoutScreenPrefab;
    [SerializeField]private MapCombatAnimation mapAnimationPrefab;
    [SerializeField]private CombatAnimationMode animationMode = CombatAnimationMode.Full;

    [SerializeField]private ExpScreen expScreenPrefab;
    [SerializeField]private LevelUpScreen levelUpScreenPrefab;


    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator Present(Unit attacker, Unit defender, CombatResult result)
    {
        switch(animationMode)
        {
            case CombatAnimationMode.Full:
            yield return ScreenManager.Instance.OpenAndWait(readoutScreenPrefab, screen => screen.Show(result));
            break;

            case CombatAnimationMode.Map:
            yield return ScreenManager.Instance.OpenAndWait(mapAnimationPrefab, animation => animation.Play(attacker,defender,result));
            break;

            case CombatAnimationMode.None:
            break;
        }
    }

    public IEnumerator PresentExp(PlayerUnit playerUnit, int beforeExp, int gainedExp)
    {
        yield return ScreenManager.Instance.OpenAndWait(expScreenPrefab, screen => screen.Show(playerUnit, beforeExp, gainedExp));
    }

    public IEnumerator PresentLevelUp(PlayerUnit playerUnit, StatBlock statBlock)
    {
        yield return ScreenManager.Instance.OpenAndWait(levelUpScreenPrefab, screen => screen.Show(playerUnit, statBlock));
    }
}
