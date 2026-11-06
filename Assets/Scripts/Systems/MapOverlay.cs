using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum OverlayColor
{
    Green,
    GreenLight,
    Red,
    RedLight,
    RedDark,
}

// Draws highlights, ranges, the path line and the reticle on top of the map.
// It only draws what it's told - it never looks at units, input or game state.
// Deciding WHAT to show is the caller's job (usually an InputState).
public class MapOverlay : MonoBehaviour
{
    public static MapOverlay Instance { get; private set; }

    [Header("Layers")]
    [SerializeField] private Tilemap selectionTilemap;   // current selection: movement range, targets, hover range
    [SerializeField] private Tilemap dangerZoneTilemap;  // toggled enemy ranges, drawn under the selection layer

    [Header("Tiles")]
    [SerializeField] private Tile green;
    [SerializeField] private Tile greenLight;
    [SerializeField] private Tile red;
    [SerializeField] private Tile redLight;
    [SerializeField] private Tile redDark;

    [Header("Cursor")]
    [SerializeField] private LineRenderer pathLine;
    [SerializeField] private Transform reticle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    //
    // SELECTION LAYER - every Show method replaces what was there before
    //
    public void HighlightCells(IEnumerable<Vector3Int> cells, OverlayColor color)
    {
        selectionTilemap.ClearAllTiles();
        Tile tile = TileFor(color);
        foreach (Vector3Int cell in cells)
            selectionTilemap.SetTile(cell, tile);
    }

    public void ShowRange(HashSet<Node> reachable, HashSet<Node> attackable, OverlayColor moveColor, OverlayColor attackColor)
    {
        selectionTilemap.ClearAllTiles();
        DrawRange(selectionTilemap, reachable, attackable, TileFor(moveColor), TileFor(attackColor));
    }

    public void ClearSelection()
    {
        selectionTilemap.ClearAllTiles();
    }

    //
    // DANGER ZONE LAYER - several enemies can be shown at once, so ranges are added rather than replaced
    //
    public void AddDangerZone(HashSet<Node> reachable, HashSet<Node> attackable)
    {
        DrawRange(dangerZoneTilemap, reachable, attackable, redDark, redDark);
    }

    public void ClearDangerZones()
    {
        dangerZoneTilemap.ClearAllTiles();
    }

    //
    // PATH LINE
    //
    // `from` is the unit's cell; `path` is the list from Pathfinder (which doesn't include the start cell)
    public void ShowPath(Vector3Int from, List<Node> path)
    {
        if (path == null || path.Count == 0)
        {
            ClearPath();
            return;
        }

        pathLine.positionCount = path.Count + 1;
        pathLine.SetPosition(0, CellCenter(from));
        for (int i = 0; i < path.Count; i++)
            pathLine.SetPosition(i + 1, CellCenter(path[i].gridPosition));
    }

    public void ClearPath()
    {
        pathLine.positionCount = 0;
    }

    //
    // RETICLE
    //
    public void ShowReticle(Vector3Int cell)
    {
        reticle.position = CellCenter(cell);
        reticle.gameObject.SetActive(true);
    }

    public void HideReticle()
    {
        reticle.gameObject.SetActive(false);
    }

    //
    // HELPERS
    //
    // Reachable tiles get moveTile; attackable tiles that aren't also reachable get attackTile
    private void DrawRange(Tilemap target, HashSet<Node> reachable, HashSet<Node> attackable, Tile moveTile, Tile attackTile)
    {
        foreach (Node node in attackable)
            if (!reachable.Contains(node)) target.SetTile(node.gridPosition, attackTile);
        foreach (Node node in reachable)
            target.SetTile(node.gridPosition, moveTile);
    }

    private Vector3 CellCenter(Vector3Int cell)
    {
        return selectionTilemap.GetCellCenterWorld(cell);
    }

    private Tile TileFor(OverlayColor color)
    {
        switch (color)
        {
            case OverlayColor.Green: return green;
            case OverlayColor.GreenLight: return greenLight;
            case OverlayColor.Red: return red;
            case OverlayColor.RedLight: return redLight;
            case OverlayColor.RedDark: return redDark;
            default: return null;
        }
    }
}
