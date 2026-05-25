using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/TerrainType", fileName = "NewTerrainType")]
public class TerrainType : ScriptableObject
{
    [System.Serializable]
    public struct ClassCost
    {
        public MovementClass movementClass;
        public int cost;
        public bool impassable;
    }

    public ClassCost[] classCosts;

    public int GetCostFor(MovementClass movementClass)
    {
        foreach (var entry in classCosts)
            if (entry.movementClass == movementClass) return entry.cost;
        return 1;
    }

    public bool IsImpassableFor(MovementClass movementClass)
    {
        foreach (var entry in classCosts)
            if (entry.movementClass == movementClass) return entry.impassable;
        return false;
    }
}
