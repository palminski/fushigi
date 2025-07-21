using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    [field: SerializeField] public Tilemap GroundTilemap { get; private set; }

    private Dictionary<Vector3Int, List<MapObject>> gridObjects = new();

    void Awake()
    {
        print("HERE");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Register(MapObject obj)
    {
        Vector3Int position = WorldToGrid(obj.transform.position);
        if (!gridObjects.ContainsKey(position))
        {
            gridObjects[position] = new List<MapObject>();
        }
        gridObjects[position].Add(obj);
    }

    public void Unregister(MapObject obj)
    {
        Vector3Int position = WorldToGrid(obj.transform.position);
        if (gridObjects.TryGetValue(position, out var list))
        {
            list.Remove(obj);
            if (list.Count == 0)
            {
                gridObjects.Remove(position);
            }
        }
    }

    public List<MapObject> GetObjectsAt(Vector3Int position)
    {
        return gridObjects.TryGetValue(position, out var list) ? list : new List<MapObject>();
    }

    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        //If tilemap no longer exists we can just clear the grid objects
        if (GroundTilemap == null)
        {
            gridObjects.Clear();
            return Vector3Int.zero;
        }
        return GroundTilemap.WorldToCell(worldPosition);
    }

    public void RefreshMap()
    {
        gridObjects.Clear();
        var mapObjects = FindObjectsOfType<MapObject>();
        foreach (MapObject mapObject in mapObjects)
        {
            Register(mapObject);
        }

    }
}
