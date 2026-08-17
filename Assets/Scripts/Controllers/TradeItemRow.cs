using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TradeItemRow : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private TextMeshProUGUI itemText;
    [SerializeField] private Image background;

    public ItemInstance item { get; private set; }
    public bool isLeftSide { get; private set; }

    private CanvasGroup canvasGroup;

    private static readonly Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private static readonly Color emptyColor  = new Color(0.1f, 0.1f, 0.1f, 0.4f);
    private static readonly Color selectedColor = new Color(0.8f, 0.7f, 0f, 0.9f);

    public void Setup(ItemInstance item, bool isLeftSide)
    {
        this.item = item;
        this.isLeftSide = isLeftSide;
        canvasGroup = GetComponent<CanvasGroup>();
        itemText.text = item != null ? TradeMenuController.GetItemLabel(item) : "- - -";
        if (background != null) background.color = item != null ? normalColor : emptyColor;
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedColor : normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TradeMenuController.Instance.OnItemClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null) return;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        TradeMenuController.Instance.BeginDrag(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (item == null) return;
        TradeMenuController.Instance.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TradeMenuController.Instance.EndDrag(eventData);
    }

    public void OnDrop(PointerEventData eventData) { }
}
