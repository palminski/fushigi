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

    private PlayerUnit playerUnit;

    void Awake()
    {
        playerUnit = GetComponent<PlayerUnit>();
    }

    public void StartMoving(List<Node> path, int moveStat = 8)
    {
        if (!isMoving)
        {
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
            playerUnit.SetInactive();
        }
    }
}
