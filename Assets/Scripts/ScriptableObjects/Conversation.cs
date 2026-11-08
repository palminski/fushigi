using System.Collections.Generic;
using UnityEngine;

// What one line does to one side of the screen
[System.Serializable]
public class PortraitSlot
{
    public Speaker speaker; // who is on this side from this line on; empty = keep whoever is already there (darkened)
    public bool darken;     // a speaker set on this line is lit (talking) unless this is ticked
    public bool clear;      // remove whoever is on this side (takes priority over speaker)

    // Lit = a speaker is set on this line and not darkened. Sides kept from earlier lines are always dark.
    public bool IsLit => speaker != null && !clear && !darken;
}

// One line of a conversation. The name plate shows whoever is lit (both names if both are, hidden if neither is).
[System.Serializable]
public class DialogueLine
{
    public PortraitSlot left = new PortraitSlot();
    public PortraitSlot right = new PortraitSlot();
    [TextArea(2, 5)] public string text;
}

// A Fire Emblem-style scene: lines played one after another by the ConversationScreen
[CreateAssetMenu(fileName = "NewConversation", menuName = "Scriptable Objects/Dialogue/Conversation")]
public class Conversation : ScriptableObject
{
    public List<DialogueLine> lines = new List<DialogueLine>();
}
