using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HoverInfoPanel : MonoBehaviour
{
    public static HoverInfoPanel Instance { get; private set; }



    [Header("Unit Info")]
    [SerializeField] private Image unitSection;
    [SerializeField] private Image unitImage;
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private TMP_Text unitClassText;
    [SerializeField] private TMP_Text unitHpText;
    [SerializeField] private TMP_Text unitStatsText;
    [SerializeField] private TMP_Text unitItemsText;
    [SerializeField] private Color playerColor;
    [SerializeField] private Color enemyColor;
    [SerializeField] private Color unusableColor = new Color(0.5f, 0.5f, 0.5f);

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
        unitNameText.text = unit.character != null ? unit.character.characterName : unit.gameObject.name;
        string unitClass = unit.character != null ? (unit.character.characterClass != null ? unit.character.characterClass.name : "Unknown") : "Unknown";
        unitClassText.text = $"{unitClass} - lv.{unit.unitAttributes.level}";
        unitImage.sprite = unit.character != null ? unit.character.portrait : null;
        unitHpText.text = $"HP  {a.currentHealth} / {a.health}";
        unitStatsText.text = $"Str {a.strength}\nSpd {a.speed}\nDef {a.defence}\nMag {a.magic}\nMov {a.movement}\nBld {a.build}\nexp {a.currentExp}";
        unitItemsText.text = BuildItemsText(unit);
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

    private string BuildItemsText(Unit unit)
    {
        if (unit.inventory.items.Count == 0) return "(no items)";
        string disabledHex = ColorUtility.ToHtmlStringRGB(unusableColor);

        var sb = new System.Text.StringBuilder();
        foreach (ItemInstance item in unit.inventory.items)
        {
            string line;
            if (item is WeaponInstance w)
            {
                line = $"{w.data.itemName}  {w.currentDurability}/{w.WeaponData.maxDurability}  Atk:{w.might}  {w.minRange}-{w.maxRange}rng";
                if (!unit.CanUse(w.WeaponData)) line = $"<color=#{disabledHex}>{line}</color>";
            }
            else
            {
                line = item.data.name;
            }
            sb.AppendLine(line);
        }
        return sb.ToString().TrimEnd();
    }
}
