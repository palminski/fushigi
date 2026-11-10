using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

// How long an unlocked topic stays known
public enum UnlockScope
{
    Conversation, // only until this conversation closes
    Run,          // for the rest of this playthrough
    Global,       // across playthroughs (survives the loop)
}

[System.Serializable]
public class TopicUnlock
{
    public Topic topic;
    public UnlockScope scope;
}

// One thing this NPC can be asked about, and what they say
[System.Serializable]
public class TopicEntry
{
    public Topic topic;
    public string alias;              // what this NPC calls the topic ("Time Loop"); empty = the topic's own name
    [TextArea(3, 8)] public string response;
    public List<TopicUnlock> unlocks = new List<TopicUnlock>(); // learned when this topic is asked

    // The name on the topic's button, and the main word linked in this NPC's text
    public string Label => string.IsNullOrEmpty(alias) && topic != null ? topic.DisplayName : alias;
}

// Morrowind-style dialogue for one NPC: a greeting, then the topics the player may be able to ask about.
// Default topics can always be asked. Unlockable ones only show up once the player can ask about them:
// the speaking unit knows the topic, it's remembered (this run or across runs), or something unlocked it earlier in this conversation.
[CreateAssetMenu(fileName = "NewNpcDialogue", menuName = "Scriptable Objects/Dialogue/NPC Dialogue")]
public class NpcDialogue : ScriptableObject
{
    public Speaker npc;
    [TextArea(3, 8)] public string greeting;
    public List<TopicEntry> defaultTopics = new List<TopicEntry>();
    [FormerlySerializedAs("topics")] public List<TopicEntry> unlockableTopics = new List<TopicEntry>();

    // Both lists, defaults first - the screen looks topics up in this
    public IEnumerable<TopicEntry> AllTopics => defaultTopics.Concat(unlockableTopics);
}
