using UnityEngine;

// The name of something dialogue can make happen in a scene: "OpenNorthGate", "SpawnReinforcements".
// Dialogue assets can't point at scene objects, so they raise one of these instead, and a DialogueEventListener
// on the scene object reacts to it. Raised when the dialogue closes, so the player sees what happens.
[CreateAssetMenu(fileName = "NewDialogueEvent", menuName = "Scriptable Objects/Dialogue/Dialogue Event")]
public class DialogueEvent : ScriptableObject
{
    [TextArea(2, 4)] public string notes; // optional: what listeners are expected to do

    public static event System.Action<DialogueEvent> Raised;
    public void Raise() => Raised?.Invoke(this);
}
