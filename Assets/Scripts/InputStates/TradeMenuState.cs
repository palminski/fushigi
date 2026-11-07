using UnityEngine;

public class TradeMenuState : InputState
{
    public PlayerUnit Initiator {get;}
    public Unit Partner {get;}

    public TradeMenuState(PlayerUnit initiator, Unit partner)
    {
        Initiator = initiator;
        Partner = partner;
    }

    public override void Enter()
    {
        TradeMenu.Instance.Show(Initiator, Partner);
    }

    public override void Exit()
    {
        TradeMenu.Instance.Hide();
    }

    public override void Tick(Vector3Int hoveredCell)
    {
        MapOverlay.Instance.HideReticle();
        HoverInfoPanel.Instance?.ShowUnit(Initiator);
    }

    public override void OnRightClick()
    {
        bool itemsChanged = TradeMenu.Instance.FinishTrade();
        if(itemsChanged) Initiator.mover.LockMove();
        InputController.Instance.OpenActionMenu(Initiator);
    }

    public override string ToString()
    {
        return $"{Initiator} is trading with {Partner}";
    }
}
