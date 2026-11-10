using UnityEngine;

// Something the player can ask NPCs about ("Loop", "Balmora"...). The asset itself is the topic's identity:
// NPC dialogue, unlocks and units' own topic lists all point at it, and remembered topics are saved by its asset name
// (so renaming a Topic asset makes old save files forget it).
[CreateAssetMenu(fileName = "NewTopic", menuName = "Scriptable Objects/Dialogue/Topic")]
public class Topic : ScriptableObject
{
    [SerializeField] private string displayName; // leave empty to use the asset's name

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
}
