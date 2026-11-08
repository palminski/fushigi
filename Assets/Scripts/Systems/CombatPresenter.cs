using System.Collections;
using UnityEngine;

public class CombatPresenter : MonoBehaviour
{
    public static CombatPresenter Instance {get; private set;}

    [SerializeField]private CombatReadoutScreen readoutScreenPrefab;

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator Present(CombatResult result)
    {
        yield return ScreenManager.Instance.OpenAndWait(readoutScreenPrefab, screen => screen.Show(result));
    }
}
