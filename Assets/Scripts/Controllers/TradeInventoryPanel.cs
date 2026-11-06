using UnityEngine;
using UnityEngine.EventSystems;

public class TradeInventoryPanel : MonoBehaviour, IDropHandler
{
    public bool isLeftSide;

    public void OnDrop(PointerEventData eventData)
    {
        TradeMenu.Instance.OnDropOnPanel(isLeftSide);
    }
}
