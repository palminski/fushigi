using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// This level's map: which tiles can be walked on and what terrain they are.
// Every level scene has exactly one, usually on the level's Grid. The systems in the
// GameController prefab read the map from here, so nothing else needs these tilemaps wired in.
public class LevelMap : MonoBehaviour
{
    public static LevelMap Instance { get; private set; }

    [System.Serializable]
    public struct TerrainLayer
    {
        public Tilemap tilemap;
        public TerrainType terrainType;
    }

    // Walkable tiles. Its grid is the one every system uses to convert between world and cell positions.
    [field: SerializeField] public Tilemap Ground { get; private set; }

    // Checked in order; the first layer with a tile at a cell decides that cell's terrain
    [field: SerializeField] public List<TerrainLayer> TerrainLayers { get; private set; } = new List<TerrainLayer>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Only remove this component - it sits on the level's Grid, which must stay
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public Vector3Int WorldToCell(Vector3 worldPosition) => Ground.WorldToCell(worldPosition);
    public Vector3 CellCenter(Vector3Int cell) => Ground.GetCellCenterWorld(cell);
    public bool HasGround(Vector3Int cell) => Ground.HasTile(cell);
}
