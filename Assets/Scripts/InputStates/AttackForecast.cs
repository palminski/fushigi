using System.Collections.Generic;

// Which weapon the player has scrolled to for the hovered enemy, and the combat forecast panel for it.
// Each state that lets the player attack owns one.
public class AttackForecast
{
    private EnemyUnit target;
    private int index;
    private WeaponInstance weapon;
    private Node attackFrom;

    public void Scroll(int direction)
    {
        index += direction;
    }

    // Shows the forecast against `enemy` for the option the player has scrolled to.
    // Hovering a different enemy starts again from the first option.
    public void Show(PlayerUnit attacker, EnemyUnit enemy, List<(WeaponInstance weapon, Node tile)> options, bool isCapture = false)
    {
        if (enemy == null || options.Count == 0)
        {
            Clear();
            return;
        }
        if (enemy != target)
        {
            target = enemy;
            index = 0;
        }

        index = ((index % options.Count) + options.Count) % options.Count; // wrap around in both directions
        (weapon, attackFrom) = options[index];
        CombatForecastPanel.Instance?.Show(attacker, enemy, weapon, attackFrom?.gridPosition, isCapture);
    }

    // Nothing to forecast: hide the panel and forget the choice
    public void Clear()
    {
        target = null;
        index = 0;
        weapon = null;
        attackFrom = null;
        CombatForecastPanel.Instance?.Hide();
    }

    // Hide the panel but remember the choice, so a click handled after leaving the state can still use it
    public void HidePanel()
    {
        CombatForecastPanel.Instance?.Hide();
    }

    // What the forecast is showing for `enemy`, or null if it isn't showing that enemy
    public WeaponInstance WeaponFor(EnemyUnit enemy) => enemy == target ? weapon : null;
    public Node AttackFromFor(EnemyUnit enemy) => enemy == target ? attackFrom : null;
}
