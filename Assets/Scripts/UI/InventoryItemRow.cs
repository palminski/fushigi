using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private Color unusableColor = new Color(0.5f,0.5f,0.5f);

    public void Setup(ItemInstance item, bool usable = true)
    {
        if (item is WeaponInstance weapon)
            itemNameText.text = $"{weapon.data.itemName}  {weapon.currentDurability}/{weapon.WeaponData.maxDurability}";
        else if (item is ConsumableInstance consumable)
            itemNameText.text = $"{consumable.data.itemName}  +{consumable.ConsumableData.healAmount} HP";
        else
            itemNameText.text = item.data.itemName;

        itemNameText.color = usable ? Color.white : unusableColor;
        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() => InventoryMenu.Instance.SelectItem(item));
    }
}
