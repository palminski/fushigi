using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HoverInfoController : MonoBehaviour
{
    public static HoverInfoController Instance { get; private set; }

    

    [Header("Unit Info")]
    [SerializeField] private Image unitSection;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text unitHpText;
    [SerializeField] private TMP_Text unitStatsText;
    [SerializeField] private TMP_Text unitItemsText;
    [SerializeField] private Color playerColor;
    [SerializeField] private Color enemyColor;

    [Header("Terrain Info")]
    [SerializeField] private Image terrainSection;
    [SerializeField] private TMP_Text terrainNameText;
    [SerializeField] private TMP_Text terrainStatsText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        unitSection.gameObject.SetActive(false);
        terrainSection.gameObject.SetActive(false);
    }

    public void ShowUnit(Unit unit)
    {
        if (unit is EnemyUnit)
        {
            unitSection.color = enemyColor;
        }
        else
        {
            unitSection.color = playerColor;
        }
        var a = unit.unitAttributes;
        unitNameText.text = unit.gameObject.name;
        unitHpText.text = $"HP  {a.currentHealth} / {a.health}";
        unitStatsText.text = $"Str {a.strength}\nSpd {a.speed}\nDef {a.defence}\nMag {a.magic}\nMov {a.movement}\nBld {a.build}";
        unitItemsText.text = BuildItemsText(unit.inventory);
        unitSection.gameObject.SetActive(true);
        terrainSection.gameObject.SetActive(false);
    }

    public void ShowTerrain(TerrainType terrain)
    {
        terrainNameText.text = string.IsNullOrEmpty(terrain.displayName) ? terrain.name : terrain.displayName;
        terrainStatsText.text = $"Def +{terrain.defenceBonus}   Avo +{terrain.avoidBonus}";
        unitSection.gameObject.SetActive(false);
        terrainSection.gameObject.SetActive(true);
    }

    public void Hide()
    {
        unitSection.gameObject.SetActive(false);
        terrainSection.gameObject.SetActive(false);
    }

    private string BuildItemsText(Inventory inventory)
    {
        if (inventory.items.Count == 0) return "(no items)";
        var sb = new System.Text.StringBuilder();
        foreach (ItemInstance item in inventory.items)
        {
            if (item is WeaponInstance w)
                sb.AppendLine($"{w.data.itemName}  {w.currentDurability}/{w.WeaponData.maxDurability}  Atk:{w.might}  {w.minRange}-{w.maxRange}rng");
            else
                sb.AppendLine(item.data.itemName);
        }
        return sb.ToString().TrimEnd();
    }
}
