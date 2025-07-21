using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapObject : MonoBehaviour
{
    public Vector3Int GridPosition => MapManager.Instance.WorldToGrid(transform.position);
    // Start is called before the first frame update
    void Start()
    {
        print(MapManager.Instance);
        MapManager.Instance.Register(this);
    }

    void OnDestroy()
    {
        MapManager.Instance.Unregister(this);
    }
}
