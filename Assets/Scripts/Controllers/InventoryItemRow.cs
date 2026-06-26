using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemRow : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;

    public void Setup(ItemInstance item)
    {
        if (item is WeaponInstance weapon)
            itemNameText.text = $"{weapon.data.itemName}  {weapon.currentDurability}/{weapon.WeaponData.maxDurability}";
        else if (item is ConsumableInstance consumable)
            itemNameText.text = $"{consumable.data.itemName}  +{consumable.ConsumableData.healAmount} HP";
        else
            itemNameText.text = item.data.itemName;

        GetComponent<Button>().onClick.RemoveAllListeners();
        GetComponent<Button>().onClick.AddListener(() => InventoryMenuController.Instance.SelectItem(item));
    }
}
