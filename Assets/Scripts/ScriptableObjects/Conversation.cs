using System.Collections.Generic;
using UnityEngine;

public enum PortraitSide { Left, Right }

// One line of a conversation. Leave the speaker empty for narration (no name or portrait).
[System.Serializable]
public class DialogueLine
{
    public Speaker speaker;
    public PortraitSide side;
    [TextArea(2, 5)] public string text;
}

// A Fire Emblem-style scene: lines played one after another by the ConversationScreen
[CreateAssetMenu(fileName = "NewConversation", menuName = "Scriptable Objects/Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();
}
