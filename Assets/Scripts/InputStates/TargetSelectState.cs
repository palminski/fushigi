using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum TargetKind
{
    Attack,
    Capture,
    Trade,
    Rescue,
    Take,
    Drop,
}

// "Highlight some cells and let the player click one of them."
// Every pick-a-target step after the action menu uses this one class;
// what actually happens on confirm/cancel is passed in by whoever starts it.
public class TargetSelectState : InputState
{
    public TargetKind Kind { get; }
    public PlayerUnit Unit { get; }

    private readonly List<Vector3Int> targetCells;
    private readonly OverlayColor highlightColor;
    private readonly Action<Vector3Int> onConfirm;
    private readonly Action onCancel;

    public TargetSelectState(TargetKind kind, PlayerUnit unit, List<Vector3Int> targetCells, OverlayColor highlightColor, Action<Vector3Int> onConfirm, Action onCancel)
    {
        Kind = kind;
        Unit = unit;
        this.targetCells = targetCells;
        this.highlightColor = highlightColor;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;
    }

    // Attack and Capture show the combat forecast while hovering targets
    public bool IsCombat => Kind == TargetKind.Attack || Kind == TargetKind.Capture;

    public bool IsTarget(Vector3Int cell) => targetCells.Contains(cell);

    public override void Enter()
    {
        MapOverlay.Instance.HighlightCells(targetCells,highlightColor);
    }

    public override void Exit()
    {
        MapOverlay.Instance.ClearSelection();
    }

    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.ShowReticle(hoveredCell);
    }

    public override void OnClick(Vector3Int cell)
    {
        if (!IsTarget(cell)) return;
        // Leave the state first so onConfirm can safely open menus or start a new state
        InputController.Instance.ReturnToIdle();
        onConfirm(cell);
    }

    public override void OnRightClick()
    {
        InputController.Instance.ReturnToIdle();
        onCancel();
    }

    public override string ToString() => $"Selecting {Kind} target";
}
