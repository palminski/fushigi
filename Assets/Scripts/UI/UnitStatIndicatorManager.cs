using UnityEngine;
using System.Collections.Generic;
public class UnitStatIndicatorManager : MonoBehaviour
{
    public static UnitStatIndicatorManager Instance {get; private set; }

    [System.Serializable]
    public class IndicatorParams
    {
        public Sprite sprite;
        public Vector2 offset;
        public Vector2 size = Vector2.one;
        public Color color = Color.white;
        public int sortingOrder;
    }

    [SerializeField] private IndicatorParams healthBarOutline;
    [SerializeField] private IndicatorParams healthBar;
    [SerializeField] private IndicatorParams heldUnitMarker;
    [SerializeField] private IndicatorParams unitHighlight;
    [SerializeField] private Color heldEnemyColor = Color.red;
    [SerializeField] private float deathFadeSeconds = 0.35f;
    [SerializeField] private float pickUpSeconds = 0.3f; // a rescued/captured unit slides onto its carrier while fading

    private class UnitIndicators
    {
        public SpriteRenderer healthBar;
        public SpriteRenderer healthBarOutline;
        public SpriteRenderer heldUnitMarker;
        public SpriteRenderer unitHighlight;
    }
    private readonly Dictionary<Unit, UnitIndicators> indicators = new Dictionary<Unit, UnitIndicators>();  

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        foreach(Unit unit in FindObjectsByType<Unit>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            AddIndicators(unit);
        }
    }

    private void AddIndicators(Unit unit)
    {

        UnitIndicators unitIndicators = new UnitIndicators
        {
            healthBar = CreateAndAssignRenderer(unit, "Health Bar", healthBar),
            healthBarOutline = CreateAndAssignRenderer(unit, "Health Bar Outline", healthBarOutline),
            heldUnitMarker = CreateAndAssignRenderer(unit, "Held Unit Marker", heldUnitMarker),
            unitHighlight = CreateAndAssignRenderer(unit, "Unit Highlight", unitHighlight),
        };

        unitIndicators.unitHighlight.enabled = false;
        indicators[unit] = unitIndicators;

        DrawHealth(unit);
        DrawHeldUnit(unit);

        unit.HealthChanged += () => DrawHealth(unit);
        unit.HeldUnitChanged += () => DrawHeldUnit(unit);
        unit.Died += () => OnUnitDied(unit);
        unit.PickedUp += carrier => FadingSprite.Spawn(unit, pickUpSeconds, carrier.transform.position);
    }

    // Leave a fading copy of the unit behind, and forget its indicators (they're destroyed along with the unit)
    private void OnUnitDied(Unit unit)
    {
        FadingSprite.Spawn(unit, deathFadeSeconds);
        indicators.Remove(unit);
    }

    private void DrawHealth(Unit unit)
    {
        ShowHealth(unit, unit.unitAttributes.currentHealth);
    }

    // Draws the bar at `health` instead of the unit's real health. Combat animations use this to drain the bar
    // as hits land, since the real damage is only applied after the animation; the next HealthChanged redraws it for real.
    public void ShowHealth(Unit unit, float health)
    {
        if (!indicators.TryGetValue(unit, out UnitIndicators unitIndicators)) return;

        int maxHealth = unit.unitAttributes.health;
        float healthPercent = maxHealth > 0 ? health / maxHealth : 0f;
        healthPercent = Mathf.Clamp01(healthPercent);

        Transform bar = unitIndicators.healthBar.transform;
        bar.localScale = new Vector3(healthBar.size.x * healthPercent, healthBar.size.y, 1f);
    }

    private void DrawHeldUnit(Unit unit)
    {
        SpriteRenderer marker = indicators[unit].heldUnitMarker;
        marker.enabled = unit.heldUnit != null;
        marker.color = unit.heldUnit is EnemyUnit ? heldEnemyColor : heldUnitMarker.color;
    }

    public void SetHighlighted(EnemyUnit enemy, bool isOn)
    {
        if (indicators.TryGetValue(enemy, out UnitIndicators unitIndicators))
        {
            unitIndicators.unitHighlight.enabled = isOn;
        }
    }

    private SpriteRenderer CreateAndAssignRenderer(Unit unit, string name, IndicatorParams parameters)
    {
        GameObject indicatorObject = new GameObject(name);
        indicatorObject.transform.SetParent(unit.transform,false);
        indicatorObject.transform.localPosition = parameters.offset;
        indicatorObject.transform.localScale = new Vector3(parameters.size.x, parameters.size.y, 1f);

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = parameters.sprite;
        spriteRenderer.color = parameters.color;
        spriteRenderer.sortingOrder = parameters.sortingOrder;
        return spriteRenderer;
    }
}
