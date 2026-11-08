using UnityEngine;

// A copy of a unit's sprite left behind when it dies, which fades out and then destroys itself.
// The real unit is gone from the game straight away; this is only what the player sees,
// so nothing waits on it - the fight, the turn and the rest of the game carry on while it fades.
public class DeathFade : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Color startColor;
    private float seconds;
    private float elapsed;

    // Leaves a fading copy of `unit`'s sprite exactly where the unit is
    public static void Spawn(Unit unit, float seconds)
    {
        SpriteRenderer original = unit.GetComponent<SpriteRenderer>();
        if (original == null) return;

        GameObject corpse = new GameObject($"{unit.name} (fading)");
        corpse.transform.SetPositionAndRotation(unit.transform.position, unit.transform.rotation);
        corpse.transform.localScale = unit.transform.lossyScale;

        SpriteRenderer copy = corpse.AddComponent<SpriteRenderer>();
        copy.sprite = original.sprite;
        copy.color = original.color;
        copy.flipX = original.flipX;
        copy.flipY = original.flipY;
        copy.sharedMaterial = original.sharedMaterial;
        copy.sortingLayerID = original.sortingLayerID;
        copy.sortingOrder = original.sortingOrder;

        DeathFade fade = corpse.AddComponent<DeathFade>();
        fade.sprite = copy;
        fade.startColor = copy.color;
        fade.seconds = seconds;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = seconds > 0 ? Mathf.Clamp01(elapsed / seconds) : 1f;

        Color color = startColor;
        color.a = startColor.a * (1f - progress);
        sprite.color = color;

        if (progress >= 1f) Destroy(gameObject);
    }
}
