using UnityEngine;

// Someone who can talk in a conversation: the name shown above their lines and the portrait shown beside them.
// Kept separate from units so NPCs, narrators and off-map characters can speak too.
[CreateAssetMenu(fileName = "NewSpeaker", menuName = "Scriptable Objects/Dialogue/Speaker")]
public class Speaker : ScriptableObject
{
    public string displayName = "Speaker";
    public Sprite portrait;
}
