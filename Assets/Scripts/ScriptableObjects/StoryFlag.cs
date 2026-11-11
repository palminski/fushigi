using UnityEngine;

// A fact about the story that can be true or not: "GaveSword", "BossDefeated", "MetTheMayor".
// Dialogue responses can require or forbid flags and set them; battle code can set them too.
// Like Topic, the asset is the flag's identity and it's saved by its asset name.
[CreateAssetMenu(fileName = "NewStoryFlag", menuName = "Scriptable Objects/Dialogue/Story Flag")]
public class StoryFlag : ScriptableObject
{
    [TextArea(2, 4)] public string notes; // optional: what this flag means, for you - never shown in game
}
