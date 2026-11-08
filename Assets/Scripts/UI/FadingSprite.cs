using UnityEngine;

// A copy of a unit's sprite that fades out or in - optionally sliding somewhere as it goes - and then destroys itself.
// Used when a unit leaves or rejoins the map (dies, is picked up, dropped or released): the real unit's state changes
// straight away, and this is only what the player sees, so nothing waits on it.
public class FadingSprite : MonoBehaviour
{
    private SpriteRenderer sprite;
    private Color fullColor;
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool fadeIn;
    private SpriteRenderer hiddenUntilArrival; // when fading in, the real unit's sprite stays hidden until the copy lands on it
    private float seconds;
    private float elapsed;

    // Fades out where the unit stands (e.g. it died)
    public static void FadeOut(Unit unit, float seconds)
    {
        Create(unit, seconds, unit.transform.position, unit.transform.position, false);
    }

    // Fades out while sliding from `from` to `to` (e.g. picked up onto its carrier, or a released captive walking off)
    public static void FadeOut(Unit unit, float seconds, Vector3 from, Vector3 to)
    {
        Create(unit, seconds, from, to, false);
    }

    // Fades in while sliding from `from` onto the unit's current position (e.g. dropped by its carrier).
    // The real unit's sprite is hidden until the copy arrives, so the player never sees two of it.
    public static void FadeIn(Unit unit, float seconds, Vector3 from)
    {
        Create(unit, seconds, from, unit.transform.position, true);
    }

    private static void Create(Unit unit, float seconds, Vector3 from, Vector3 to, bool fadeIn)
    {
        SpriteRenderer original = unit.GetComponent<SpriteRenderer>();
        if (original == null) return;

        GameObject copyObject = new GameObject($"{unit.name} (fading)");
        copyObject.transform.SetPositionAndRotation(from, unit.transform.rotation);
        copyObject.transform.localScale = unit.transform.lossyScale;

        SpriteRenderer copy = copyObject.AddComponent<SpriteRenderer>();
        copy.sprite = original.sprite;
        copy.color = original.color;
        copy.flipX = original.flipX;
        copy.flipY = original.flipY;
        copy.sharedMaterial = original.sharedMaterial;
        copy.sortingLayerID = original.sortingLayerID;
        copy.sortingOrder = original.sortingOrder + 1; // above the unit it may be sliding onto or out of

        FadingSprite fade = copyObject.AddComponent<FadingSprite>();
        fade.sprite = copy;
        fade.fullColor = original.color;
        fade.startPosition = from;
        fade.endPosition = to;
        fade.fadeIn = fadeIn;
        fade.seconds = seconds;

        if (fadeIn)
        {
            original.enabled = false;
            fade.hiddenUntilArrival = original;
        }
        fade.Apply(0f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = seconds > 0 ? Mathf.Clamp01(elapsed / seconds) : 1f;
        Apply(progress);

        if (progress >= 1f)
        {
            if (hiddenUntilArrival != null) hiddenUntilArrival.enabled = true;
            Destroy(gameObject);
        }
    }

    private void Apply(float progress)
    {
        transform.position = Vector3.Lerp(startPosition, endPosition, progress);

        Color color = fullColor;
        color.a = fullColor.a * (fadeIn ? progress : 1f - progress);
        sprite.color = color;
    }
}
