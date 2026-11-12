using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapObjectIndicatorManager : MonoBehaviour
{
    public enum IndicatorType
    {
        Talk,
    }
    public static MapObjectIndicatorManager Instance { get; private set; }
    
    [Header("Indicators")]
    [SerializeField] private IndicatorParams canTalkIndicator;

    private class MapObjectIndicators
    {
        public SpriteRenderer canTalkToIndicator;
    }

    private readonly Dictionary<MapObject, MapObjectIndicators> indicators = new Dictionary<MapObject, MapObjectIndicators>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void TurnOnIndicator(MapObject mapObject, IndicatorType indicatorType)
    {
        SetIndicator(mapObject, indicatorType, true);
    }

    public void TurnOffIndicator(MapObject mapObject, IndicatorType indicatorType)
    {
        SetIndicator(mapObject, indicatorType, false);
    }

    public void SetIndicator(MapObject mapObject, IndicatorType indicatorType, bool isTurningOn)
    {
        if(mapObject == null)
        {
            indicators.Remove(mapObject);
            return;
        }
        if (!indicators.TryGetValue(mapObject, out MapObjectIndicators objectIndicators))
        {
            if (!isTurningOn) return;
            objectIndicators = new MapObjectIndicators();
            indicators[mapObject] = objectIndicators;
        }
        switch (indicatorType)
        {
            case IndicatorType.Talk:
                if(objectIndicators.canTalkToIndicator == null)
                {
                    if (!isTurningOn) return;
                    objectIndicators.canTalkToIndicator = Indicators.CreateAndAssignRenderer(mapObject.transform, "canTalkTo", canTalkIndicator);
                }
                objectIndicators.canTalkToIndicator.enabled = isTurningOn;
                break;
        }
    }

    public void HideAll(IndicatorType type)
    {
        RemoveDestroyedFromIndicators();
        foreach(MapObject mapObject in indicators.Keys.ToList()) TurnOffIndicator(mapObject, type);
    }

    public void RemoveDestroyedFromIndicators()
    {
        foreach(MapObject nulledMapObject in indicators.Keys.Where(key => key == null).ToList()) indicators.Remove(nulledMapObject);
    }
}
