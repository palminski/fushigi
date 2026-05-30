using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Mover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public Tilemap tilemap;

    [HideInInspector] public bool isMoving = false;

    private Vector3 currentFinalTarget;

    public PlayerUnit playerUnit;
    private Vector3 startWorldPosition;
    private EnemyUnit queuedAttackTarget;
    private Weapon queuedAttackWeapon;

    void Awake()
    {
        playerUnit = GetComponent<PlayerUnit>();
    }

    public void StartMoving(List<Node> path, int moveStat = 8)
    {
        if (!isMoving)
        {
            startWorldPosition = transform.position;
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
                currentFinalTarget = tilemap.GetCellCenterWorld(path[path.Count - 1].gridPosition);
            }
            foreach (Node node in path)
            {
                Vector3 targetPosition = tilemap.GetCellCenterWorld(node.gridPosition);
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
            PathfinderController.Instance.GenerateGrid();
            if (queuedAttackTarget != null)
            {
                EnemyUnit target = queuedAttackTarget;
                Weapon weapon = queuedAttackWeapon;
                queuedAttackTarget = null;
                queuedAttackWeapon = null;

                playerUnit.Attack(target,weapon);
                playerUnit.SetInactive();
            } 
            else 
            {
                ActionMenuController.Instance.ShowMenu(playerUnit);
            }
        }
    }

    public void DeactivatePlayer()
    {
        startWorldPosition = transform.position;
        if (playerUnit) ActionMenuController.Instance.ShowMenu(playerUnit);
    }

    public void CancelMove()
    {
        transform.position = startWorldPosition;
        PathfinderController.Instance.GenerateGrid();
    }

    public void QueueAttack(EnemyUnit target, Weapon weapon)
    {
        queuedAttackTarget = target;
        queuedAttackWeapon = weapon;
    }
}
