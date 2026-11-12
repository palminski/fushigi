using UnityEngine;
[System.Serializable]
    public class IndicatorParams
    {
        public Sprite sprite;
        public Vector2 offset;
        public Vector2 size = Vector2.one;
        public Color color = Color.white;
        public int sortingOrder;
    }
static class Indicators
{
    public static SpriteRenderer CreateAndAssignRenderer(Transform transform, string name, IndicatorParams parameters)
    {
        GameObject indicatorObject = new GameObject(name);
        indicatorObject.transform.SetParent(transform,false);
        indicatorObject.transform.localPosition = parameters.offset;
        indicatorObject.transform.localScale = new Vector3(parameters.size.x, parameters.size.y, 1f);

        SpriteRenderer spriteRenderer = indicatorObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = parameters.sprite;
        spriteRenderer.color = parameters.color;
        spriteRenderer.sortingOrder = parameters.sortingOrder;
        return spriteRenderer;
    }
}
