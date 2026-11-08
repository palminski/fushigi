using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Plays a fight on the map itself (units lunge, flash, show damage...). It's an overlay with no visuals of its own:
// being an overlay freezes the map while it plays, and a click skips to the end.
public class MapCombatAnimation : OverlayScreen
{
    [Header("Lunge")]
    [SerializeField] private float lungeDistance = 0.3f;   // in tiles - keep under 0.5 so the unit never leaves its tile
    [SerializeField] private float lungeSeconds = 0.12f;   // each way
    [SerializeField] private float pauseBetweenSwings = 0.25f;

    [Header("Hit flash")]
    [SerializeField] private Color hitFlashTint = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private float hitFlashSeconds = 0.1f;
    [SerializeField] private float healthDrainSeconds = 0.35f;

    [Header("Critical hit")]
    [SerializeField] private Color critFlashTint = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private float critShakeStrength = 0.08f; // in tiles
    [SerializeField] private float critSeconds = 0.35f;
    [SerializeField] private int critFlashes = 3;

    [Header("Damage numbers")]
    [SerializeField] private TMP_FontAsset popupFont; // leave empty to use TextMeshPro's default font
    [SerializeField] private float popupFontSize = 4f;
    [SerializeField] private Color damageColor = Color.white;
    [SerializeField] private Color critColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color missColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private float critSizeMultiplier = 1.4f;
    [SerializeField] private Vector2 popupOffset = new Vector2(0f, 0.4f); // from the unit's centre, in tiles
    [SerializeField] private float popupRise = 0.5f;
    [SerializeField] private float popupSeconds = 0.7f;
    [SerializeField] private int popupSortingOrder = 100; // above units and their health bars

    private readonly List<GameObject> popups = new List<GameObject>();

    private Unit attacker;
    private Unit defender;
    private CombatResult result;
    private bool skipping;

    public void Play(Unit attacker, Unit defender, CombatResult result)
    {
        this.attacker = attacker;
        this.defender = defender;
        this.result = result;
        StartCoroutine(PlayAll());
    }

    // Don't close straight away: every animation below jumps to its end when skipping is set,
    // so units are always put back exactly where they belong before the screen closes
    public override void OnClick()
    {
        skipping = true;
    }

    private IEnumerator PlayAll()
    {
        foreach (CombatEvent combatEvent in result.events)
        {
            if (skipping) break;
            Unit striker = IsAttackersSwing(combatEvent) ? attacker : defender;
            Unit target = striker == attacker ? defender : attacker;
            yield return PlayHit(striker, target, combatEvent);
        }

        // Let the last damage number finish fading before the screen closes (and clears them up)
        while (popups.Count > 0 && !skipping) yield return null;
        Close();
    }

    // One swing: attack animation, lunge in, react to the hit, lunge back
    private IEnumerator PlayHit(Unit striker, Unit target, CombatEvent combatEvent)
    {
        Vector3 home = striker.transform.position;
        Vector3 lungePoint = home + (target.transform.position - home).normalized * lungeDistance;

        yield return PlayAttackAnimation(striker);
        yield return Move(striker.transform, home, lungePoint, lungeSeconds);

        if (!combatEvent.didHit)
        {
            ShowPopup(target, "Miss", missColor, 1f);
        }
        else
        {
            if (combatEvent.wasCritical) ShowPopup(target, combatEvent.damage + "!", critColor, critSizeMultiplier);
            else ShowPopup(target, combatEvent.damage.ToString(), damageColor, 1f);

            // The bar drains while the target reacts: start it alongside, then wait for both to finish
            Coroutine drain = StartCoroutine(DrainHealth(target, combatEvent.hitHpBefore, combatEvent.hitHpAfter));
            if (combatEvent.wasCritical) yield return PlayCriticalHit(target);
            else yield return Flash(target, hitFlashTint, hitFlashSeconds);
            yield return drain;
        }

        yield return Move(striker.transform, lungePoint, home, lungeSeconds);
        yield return Wait(pauseBetweenSwings);
    }

    // STUB: play the unit's own attack animation once units have animated sprites, e.g.
    //     Animator animator = unit.GetComponent<Animator>();
    //     if (animator != null) { animator.SetTrigger("Attack"); yield return Wait(attackAnimationSeconds); }
    // No unit has an Animator yet, so for now this does nothing.
    private IEnumerator PlayAttackAnimation(Unit unit)
    {
        yield break;
    }

    // STUB: a critical hit shakes the target while flashing it a few times.
    // Nothing rolls critical hits yet (CombatEvent.wasCritical is always false), so this is ready for when something does.
    private IEnumerator PlayCriticalHit(Unit target)
    {
        SpriteRenderer sprite = target.GetComponent<SpriteRenderer>();
        Color originalColor = sprite.color;
        Vector3 home = target.transform.position;
        float flashLength = critSeconds / (critFlashes * 2);

        for (float t = 0; t < critSeconds && !skipping; t += Time.deltaTime)
        {
            target.transform.position = home + (Vector3)(Random.insideUnitCircle * critShakeStrength);
            bool flashOn = (int)(t / flashLength) % 2 == 0;
            sprite.color = flashOn ? originalColor * critFlashTint : originalColor;
            yield return null;
        }

        target.transform.position = home;
        sprite.color = originalColor;
    }

    // Tints the unit's sprite for a moment, then restores whatever colour it had before (greyed-out units stay grey)
    private IEnumerator Flash(Unit unit, Color tint, float seconds)
    {
        SpriteRenderer sprite = unit.GetComponent<SpriteRenderer>();
        Color originalColor = sprite.color;
        sprite.color = originalColor * tint;
        yield return Wait(seconds);
        sprite.color = originalColor;
    }

    // Floating text above `unit` that rises and fades on its own, without holding up the fight
    private void ShowPopup(Unit unit, string text, Color color, float sizeMultiplier)
    {
        if (skipping) return;

        GameObject popup = new GameObject("Combat Popup");
        popup.transform.position = unit.transform.position + (Vector3)popupOffset;

        TextMeshPro label = popup.AddComponent<TextMeshPro>();
        if (popupFont != null) label.font = popupFont;
        label.text = text;
        label.color = color;
        label.fontSize = popupFontSize * sizeMultiplier;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.sortingOrder = popupSortingOrder;

        popups.Add(popup);
        StartCoroutine(FloatAndFade(popup, label));
    }

    private IEnumerator FloatAndFade(GameObject popup, TextMeshPro label)
    {
        Vector3 start = popup.transform.position;
        Color color = label.color;
        for (float t = 0; t < popupSeconds && !skipping; t += Time.deltaTime)
        {
            float progress = t / popupSeconds;
            popup.transform.position = start + Vector3.up * (popupRise * progress);
            color.a = 1f - progress * progress; // stays mostly solid, then fades quickly at the end
            label.color = color;
            yield return null;
        }
        popups.Remove(popup);
        Destroy(popup);
    }

    // Popups float on their own coroutines, which stop when this screen is destroyed - clear up any still showing
    void OnDestroy()
    {
        foreach (GameObject popup in popups)
        {
            if (popup != null) Destroy(popup);
        }
    }

    // Slides the unit's health bar from `from` HP down to `to` HP, always finishing exactly at `to` (instantly when skipping)
    private IEnumerator DrainHealth(Unit unit, int from, int to)
    {
        for (float t = 0; t < healthDrainSeconds && !skipping; t += Time.deltaTime)
        {
            UnitStatIndicatorManager.Instance.ShowHealth(unit, Mathf.Lerp(from, to, t / healthDrainSeconds));
            yield return null;
        }
        UnitStatIndicatorManager.Instance.ShowHealth(unit, to);
    }

    // Slides `mover` from `from` to `to` over `seconds`, always finishing exactly at `to` (instantly when skipping)
    private IEnumerator Move(Transform mover, Vector3 from, Vector3 to, float seconds)
    {
        for (float t = 0; t < seconds && !skipping; t += Time.deltaTime)
        {
            mover.position = Vector3.Lerp(from, to, t / seconds);
            yield return null;
        }
        mover.position = to;
    }

    // Waits `seconds`, or stops waiting as soon as the player skips
    private IEnumerator Wait(float seconds)
    {
        for (float t = 0; t < seconds && !skipping; t += Time.deltaTime)
        {
            yield return null;
        }
    }

    // CombatEvent.hitWasPlayer means "the unit swinging is a player unit"
    private bool IsAttackersSwing(CombatEvent combatEvent) => combatEvent.hitWasPlayer == (attacker is PlayerUnit);
}
