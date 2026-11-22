using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatForecastPanel : MonoBehaviour
{
    public static CombatForecastPanel Instance { get; private set; }

    [SerializeField] private GameObject panel;

    [Header("Player Side")]
    [SerializeField] private Image playerPortrait;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text playerWeaponText;
    [SerializeField] private TMP_Text playerDamageText;
    [SerializeField] private TMP_Text playerHitText;

    [Header("Enemy Side")]
    [SerializeField] private Image enemyPortrait;
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

    public void Show(PlayerUnit attacker, EnemyUnit defender, WeaponInstance weapon, Vector3Int? fromPosition = null, bool isCaptureAttempt = false)
    {
        CombatPreview preview = CombatCalculator.Preview(attacker, defender, weapon, fromPosition, isCaptureAttempt);

        playerNameText.text = attacker.character != null ? attacker.character.characterName : attacker.gameObject.name;
        playerPortrait.sprite = attacker.character != null ? attacker.character.portrait : null;

        playerWeaponText.text = $"{weapon.data.itemName} ({weapon.currentDurability}/{weapon.WeaponData.maxDurability})";
        playerDamageText.text = preview.attackerDoubles ? $"{preview.damageDealt} (x2)" : preview.damageDealt.ToString();
        playerHitText.text = $"{preview.attackerHitChance}%";

        WeaponInstance defWeapon = defender.EquippedWeapon;
        enemyNameText.text = defender.gameObject.name;

        enemyNameText.text = defender.character != null ? defender.character.characterName : defender.gameObject.name;
        enemyPortrait.sprite = defender.character != null ? defender.character.portrait : null;
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
