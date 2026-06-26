using UnityEngine;
using UnityEngine.UI;

public class InventoryMenuController : MonoBehaviour
{
    public static InventoryMenuController Instance { get; private set; }

    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Transform itemListParent;
    [SerializeField] private GameObject itemRowPrefab;

    [Header("Item Submenu")]
    [SerializeField] private GameObject subMenuPanel;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button discardButton;

    private PlayerUnit pendingUnit;
    private ItemInstance selectedItem;

    public bool isMenuOpen => (menuPanel != null && menuPanel.activeSelf) || isSubMenuOpen;
    public bool isSubMenuOpen => subMenuPanel != null && subMenuPanel.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        menuPanel.SetActive(false);
        subMenuPanel.SetActive(false);
    }

    public void Show(PlayerUnit unit)
    {
        pendingUnit = unit;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(unit.transform.position);
        menuPanel.GetComponent<RectTransform>().position = screenPosition + new Vector3(35f, 0f, 0f);
        subMenuPanel.SetActive(false);
        menuPanel.SetActive(true);
        Refresh();
    }

    public void Refresh()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        foreach (ItemInstance item in pendingUnit.inventory.items)
        {
            GameObject row = Instantiate(itemRowPrefab, itemListParent);
            row.GetComponent<InventoryItemRow>().Setup(item);
        }

        menuPanel.SetActive(true);
        subMenuPanel.SetActive(false);
        selectedItem = null;
    }

    public void SelectItem(ItemInstance item)
    {
        selectedItem = item;

        bool isWeapon = item is WeaponInstance;
        bool isConsumable = item is ConsumableInstance;
        bool isEquipped = pendingUnit.inventory.items.IndexOf(item) == 0;

        equipButton.gameObject.SetActive(isWeapon);
        if (isWeapon) equipButton.interactable = !isEquipped;

        useButton.gameObject.SetActive(isConsumable);

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(pendingUnit.transform.position);
        subMenuPanel.GetComponent<RectTransform>().position = screenPosition + new Vector3(35f, 0f, 0f);
        menuPanel.SetActive(false);
        subMenuPanel.SetActive(true);
    }

    public void OnEquipClicked()
    {
        pendingUnit.inventory.Equip((WeaponInstance)selectedItem);
        Refresh();
    }

    public void OnUseClicked()
    {
        ((ConsumableInstance)selectedItem).Use(pendingUnit);
        pendingUnit.inventory.items.Remove(selectedItem);
        CloseAndEndTurn();
    }

    public void OnDiscardClicked()
    {
        pendingUnit.inventory.items.Remove(selectedItem);
        Refresh();
    }

    public void CloseSubMenu()
    {
        subMenuPanel.SetActive(false);
        selectedItem = null;
        menuPanel.SetActive(true);
    }

    public void Close()
    {
        menuPanel.SetActive(false);
        subMenuPanel.SetActive(false);
        pendingUnit = null;
        selectedItem = null;
        ActionMenuController.Instance.ReopenMenu();
    }

    public void CloseAndEndTurn()
    {
        PlayerUnit unit = pendingUnit;
        menuPanel.SetActive(false);
        subMenuPanel.SetActive(false);
        pendingUnit = null;
        selectedItem = null;
        ActionMenuController.Instance.HideMenu();
        unit.SetInactive();
    }
}
