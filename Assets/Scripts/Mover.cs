using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [HideInInspector] public bool isMoving = false;

    private Vector3 currentFinalTarget;

    public PlayerUnit playerUnit;
    private Vector3 startWorldPosition;
    private EnemyUnit queuedAttackTarget;
    private WeaponInstance queuedAttackWeapon;

    // False once the unit has done something this turn that can't be taken back (rescue, trade, drop...),
    // so it can no longer be sent back to where it started
    public bool CanUndoMove { get; private set; } = true;

    public void LockMove()
    {
        CanUndoMove = false;
    }

    void Awake()
    {
        playerUnit = GetComponent<PlayerUnit>();
    }

    public void StartMoving(List<Node> path, int moveStat = 8)
    {
        if (!isMoving)
        {
            startWorldPosition = transform.position;
            CanUndoMove = true;
            StartCoroutine(FollowPath(path, moveStat));
            return;
        }
        StopAllCoroutines();
        isMoving = false;
        transform.position = currentFinalTarget;
    }

    public IEnumerator FollowPath(List<Node> path, int moveStat = 8)
    {
        {
            isMoving = true;
            if (path.Count > 0)
            {
                currentFinalTarget = LevelMap.Instance.CellCenter(path[path.Count - 1].gridPosition);
            }
            foreach (Node node in path)
            {
                Vector3 targetPosition = LevelMap.Instance.CellCenter(node.gridPosition);
                while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                transform.position = targetPosition;
            }
            isMoving = false;
            Pathfinder.Instance.GenerateGrid();
            if (queuedAttackTarget != null)
            {
                EnemyUnit target = queuedAttackTarget;
                WeaponInstance weapon = queuedAttackWeapon;
                queuedAttackTarget = null;
                queuedAttackWeapon = null;

                yield return playerUnit.AttackAndEndTurn(target, weapon);
            } 
            else if (playerUnit != null)
            {
                InputController.Instance.OpenActionMenu(playerUnit);
            }
        }
    }

    public void DeactivatePlayer()
    {
        startWorldPosition = transform.position;
        CanUndoMove = true;
        if (playerUnit) InputController.Instance.OpenActionMenu(playerUnit);
    }

    public void CancelMove()
    {
        transform.position = startWorldPosition;
        Pathfinder.Instance.GenerateGrid();
    }

    public void QueueAttack(EnemyUnit target, WeaponInstance weapon)
    {
        queuedAttackTarget = target;
        queuedAttackWeapon = weapon;
    }
}
