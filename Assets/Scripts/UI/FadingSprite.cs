using UnityEngine;

// A copy of a unit's sprite that fades out - optionally sliding somewhere as it goes - and then destroys itself.
// Used when a unit leaves the map (it dies, or is picked up): the real unit is gone from the game straight away,
// and this is only what the player sees, so nothing waits on it.
public class FadingSprite : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Color startColor;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float seconds;
    private float elapsed;

    // Fades out where the unit stands
    public static void Spawn(Unit unit, float seconds)
    {
        Spawn(unit, seconds, unit.transform.position);
    }

    // Fades out while sliding from where the unit stands to `moveTo`
    public static void Spawn(Unit unit, float seconds, Vector3 moveTo)
    {
        SpriteRenderer original = unit.GetComponent<SpriteRenderer>();
        if (original == null) return;

        GameObject copyObject = new GameObject($"{unit.name} (fading)");
        copyObject.transform.SetPositionAndRotation(unit.transform.position, unit.transform.rotation);
        copyObject.transform.localScale = unit.transform.lossyScale;

        SpriteRenderer copy = copyObject.AddComponent<SpriteRenderer>();
        copy.sprite = original.sprite;
        copy.color = original.color;
        copy.flipX = original.flipX;
        copy.flipY = original.flipY;
        copy.sharedMaterial = original.sharedMaterial;
        copy.sortingLayerID = original.sortingLayerID;
        copy.sortingOrder = original.sortingOrder + 1; // above the unit it may be sliding onto

        FadingSprite fade = copyObject.AddComponent<FadingSprite>();
        fade.sprite = copy;
        fade.startColor = copy.color;
        fade.startPosition = unit.transform.position;
        fade.endPosition = moveTo;
        fade.seconds = seconds;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = seconds > 0 ? Mathf.Clamp01(elapsed / seconds) : 1f;

        transform.position = Vector3.Lerp(startPosition, endPosition, progress);

        Color color = startColor;
        color.a = startColor.a * (1f - progress);
        sprite.color = color;

        if (progress >= 1f) Destroy(gameObject);
    }
}
