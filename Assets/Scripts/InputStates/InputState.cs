using UnityEngine;

// One "mode" the player can be in (e.g. picking an attack target).
// InputController forwards input to whichever state is active, so each mode's
// behaviour lives in its own class instead of in a chain of if/else checks.
public abstract class InputState
{
    // Called once when this state becomes active / stops being active.
    public virtual void Enter() {}
    public virtual void Exit() {}
    public virtual void Tick(Vector3Int hoveredCell){}
    public virtual void OnClick(Vector3Int cell) {}
    public virtual void OnRightClick() {}
}
