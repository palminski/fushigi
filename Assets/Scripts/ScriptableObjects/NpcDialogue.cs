using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

// How long something learned or set in dialogue lasts (topic unlocks and story flags)
public enum MemoryScope
{
    Conversation, // only until this conversation closes
    Run,          // for the rest of this playthrough
    Global,       // across playthroughs (survives the loop)
}

[System.Serializable]
public class TopicUnlock
{
    public Topic topic;
    public MemoryScope scope;
}

[System.Serializable]
public class FlagChange
{
    public StoryFlag flag;
    public MemoryScope scope;
}

// What happens when a response is given or a choice is picked
[System.Serializable]
public class DialogueOutcome
{
    public List<TopicUnlock> unlocks = new List<TopicUnlock>();
    public List<FlagChange> setFlags = new List<FlagChange>();
    public List<Item> giveItems = new List<Item>();               // to the speaking unit (or the convoy)
    public List<DialogueEvent> raiseEvents = new List<DialogueEvent>(); // raised when the dialogue closes
}

// One answer the player can give when a response asks something
[System.Serializable]
public class DialogueChoice
{
    public string label;                       // the button text
    [TextArea(2, 6)] public string reply;      // what the NPC says back (optional)
    public DialogueOutcome outcome = new DialogueOutcome();
    public Topic continueTo;                   // optional: then show this topic's response (it can ask more questions)
}

// One possible answer to a topic. A topic's responses are checked in order and the first whose flags pass is used,
// so put special cases first and an unconditional fallback last.
[System.Serializable]
public class TopicResponse
{
    public List<StoryFlag> requires = new List<StoryFlag>(); // all of these must be set
    public List<StoryFlag> forbids = new List<StoryFlag>();  // none of these may be set
    [TextArea(3, 8)] public string text;
    public DialogueOutcome outcome = new DialogueOutcome();
    public List<DialogueChoice> choices = new List<DialogueChoice>(); // any: the player must pick one before doing anything else
}

// One thing this NPC can be asked about, and the ways they can answer
[System.Serializable]
public class TopicEntry
{
    public Topic topic;
    public string alias; // what this NPC calls the topic ("Time Loop"); empty = the topic's own name
    public List<TopicResponse> responses = new List<TopicResponse>();

    // The name on the topic's button, and the main word linked in this NPC's text
    public string Label => string.IsNullOrEmpty(alias) && topic != null ? topic.DisplayName : alias;
}

// Morrowind-style dialogue for one NPC: a greeting, then the topics the player may be able to ask about.
// Default topics can always be asked. Unlockable ones only show up once the player can ask about them:
// the speaking unit knows the topic, it's remembered (this run or across runs), or something unlocked it earlier in this conversation.
// Hidden topics never get a button: they're reached through a choice's "continue to" or an explicit [text](Topic) link.
[CreateAssetMenu(fileName = "NewNpcDialogue", menuName = "Scriptable Objects/Dialogue/NPC Dialogue")]
public class NpcDialogue : ScriptableObject
{
    public Speaker npc;
    [TextArea(3, 8)] public string greeting;
    public List<TopicEntry> defaultTopics = new List<TopicEntry>();
    [FormerlySerializedAs("topics")] public List<TopicEntry> unlockableTopics = new List<TopicEntry>();
    public List<TopicEntry> hiddenTopics = new List<TopicEntry>();

    // The topics that can get a button in the topic list (defaults first)
    public IEnumerable<TopicEntry> ListedTopics => defaultTopics.Concat(unlockableTopics);

    // Every topic, hidden ones included - for looking an entry up by its topic
    public IEnumerable<TopicEntry> AllTopics => ListedTopics.Concat(hiddenTopics);
}
