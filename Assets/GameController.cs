using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    Player,
    Enemy,
}
public class GameController : MonoBehaviour
{
    public static GameController Instance { get; private set; }

    public GamePhase gamePhase = GamePhase.Player;

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
        ChangePhase();
    }

    public void ChangePhase()
    {
        var playerUnits = FindObjectsOfType<PlayerUnit>();
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
        }
    }

    public bool CheckIfAllPlayersHaveActed()
    {

        var playerUnits = FindObjectsOfType<PlayerUnit>();
        foreach (PlayerUnit playerUnit in playerUnits)
        {
            if (playerUnit.canAct)
            {
                return false;
            }
        }
        return true;


    }
}
