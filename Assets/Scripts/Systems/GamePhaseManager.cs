using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Player,
    Enemy,
    Ally,
}
public class GamePhaseManager : MonoBehaviour
{
    public static GamePhaseManager Instance { get; private set; }

    public GamePhase gamePhase = GamePhase.Player;
    [SerializeField] private PhaseDisplayScreen phaseDisplayScreen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PresentGamePhaseBanner(gamePhase));
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CheckAndChangePhase()
    {
        if (CheckIfAllPlayersHaveActed())
        {
            ChangePhase();
        }
    }

    public void PerformEnemyPhase()
    {

        StartCoroutine(MoveEnemies());
    }

    private IEnumerator MoveEnemies()
    {
        yield return PresentGamePhaseBanner(gamePhase);
        var enemyUnits = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
        foreach (EnemyUnit enemyUnit in enemyUnits)
        {
            if (enemyUnit == null) continue;
            enemyUnit.PerformTurnMovement();
            while (enemyUnit != null && enemyUnit.mover.isMoving)
            {
                yield return null;
            }
            if (enemyUnit != null)
                yield return StartCoroutine(enemyUnit.PerformTurnActionCoroutine());
        }
        ChangePhase();
    }

    public void ChangePhase()
    {
        var playerUnits = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
        foreach (PlayerUnit playerUnit in playerUnits)
        {
            playerUnit.SetActive();
        }
        if (gamePhase == GamePhase.Player)
        {
            gamePhase = GamePhase.Enemy;
            PerformEnemyPhase();
        }
        else
        {
            gamePhase = GamePhase.Player;
            StartCoroutine(PresentGamePhaseBanner(gamePhase));
        }
    }

    public bool CheckIfAllPlayersHaveActed()
    {

        var playerUnits = FindObjectsByType<PlayerUnit>(FindObjectsSortMode.None);
        foreach (PlayerUnit playerUnit in playerUnits)
        {
            if (playerUnit.canAct)
            {
                return false;
            }
        }
        return true;


    }

    private IEnumerator PresentGamePhaseBanner(GamePhase phase)
    {
        yield return ScreenManager.Instance.OpenAndWait(phaseDisplayScreen, screen => screen.Show(phase));
    }
}
