using TMPro;
using UnityEngine;

public class CombatForecastController : MonoBehaviour
{
    public static CombatForecastController Instance { get; private set; }

    [SerializeField] private GameObject panel;

    [Header("Player Side")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerWeaponText;
    [SerializeField] private TMP_Text playerDamageText;
    [SerializeField] private TMP_Text playerHitText;

    [Header("Enemy Side")]
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyWeaponText;
    [SerializeField] private TMP_Text enemyDamageText;
    [SerializeField] private TMP_Text enemyHitText;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(PlayerUnit attacker, EnemyUnit defender, WeaponInstance weapon, Vector3Int? fromPosition = null)
    {
        CombatPreview preview = CombatCalculator.Preview(attacker, defender, weapon, fromPosition);

        playerNameText.text = attacker.gameObject.name;
        playerWeaponText.text = $"{weapon.data.itemName} ({weapon.currentDurability}/{weapon.WeaponData.maxDurability})";
        playerDamageText.text = preview.attackerDoubles ? $"{preview.damageDealt} (x2)" : preview.damageDealt.ToString();
        playerHitText.text = $"{preview.attackerHitChance}%";

        WeaponInstance defWeapon = defender.inventory.EquippedWeapon;
        enemyNameText.text = defender.gameObject.name;
        if (preview.defenderCanCounter && defWeapon != null)
        {
            enemyWeaponText.text = $"{defWeapon.data.itemName} ({defWeapon.currentDurability}/{defWeapon.WeaponData.maxDurability})";
            enemyDamageText.text = preview.defenderDoubles ? $"{preview.damageReceived} (x2)" : preview.damageReceived.ToString();
            enemyHitText.text = $"{preview.defenderHitChance}%";
        }
        else
        {
            enemyWeaponText.text = "---";
            enemyDamageText.text = "---";
            enemyHitText.text = "---";
        }

        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);
}
