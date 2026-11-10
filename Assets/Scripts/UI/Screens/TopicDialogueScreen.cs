using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TopicDialogueScreen : OverlayScreen
{
    [Header("Speaker Data")]
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerPortrait;

    [Header("Log")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private ScrollRect logScroll;
    [SerializeField] private Color topicHeadingColor = Color.blue;
    [SerializeField] private Color linkColor = new Color(0.55f, 0.8f, 1f);

    [Header("Topics")]
    [SerializeField] private Transform topicList;
    [SerializeField] private Button topicButtonPrefab;
    [SerializeField] private Button goodbyeButton;

    private class LogEntry
    {
        public string heading;
        public string text;
        public Topic topic; // the topic this answers (null for the greeting) - never linked inside its own answer
    }

    private NpcDialogue dialogue;
    private PlayerUnit initiatingUnit;
    private readonly List<Topic> unlockedThisConversation = new List<Topic>();
    private readonly List<LogEntry> log = new List<LogEntry>();

    public void Show(NpcDialogue dialogue, PlayerUnit initiatingUnit)
    {
        this.dialogue = dialogue;
        this.initiatingUnit = initiatingUnit;

        Speaker speaker = dialogue.npc;
        speakerNameText.text = speaker != null ? speaker.displayName : dialogue.name;
        speakerPortrait.sprite = speaker!= null ? speaker.portrait : null;
        speakerPortrait.enabled = speakerPortrait.sprite != null;

        goodbyeButton.onClick.AddListener(Close);

        AddToLog(null, dialogue.greeting);
        RebuildTopicList();
    }

    private bool CanAsk(TopicEntry topicEntry)
    {
        if(topicEntry.topic == null) return false;
        if(dialogue.defaultTopics.Contains(topicEntry)) return true;

        return unlockedThisConversation.Contains(topicEntry.topic)
        || GameSaveManager.TopicKnown(topicEntry.topic)
        || (initiatingUnit != null && initiatingUnit.personalTopics.Contains(topicEntry.topic));
    }

    private List<TopicEntry> AskableTopics()
    {
        return dialogue.AllTopics.Where(CanAsk).OrderBy(entry => entry.Label).ToList();
    }

    private void RebuildTopicList()
    {
        foreach (Transform child in topicList) Destroy(child.gameObject);

        foreach (TopicEntry entry in AskableTopics())
        {
            Button button = Instantiate(topicButtonPrefab, topicList);
            button.GetComponentInChildren<TMP_Text>().text = entry.Label;
            button.onClick.AddListener(() => Ask(entry));
        }
    }

    // Unlocks are learned before the response is logged, so the response can already link the topics it unlocks
    private void Ask(TopicEntry topicEntry)
    {
        foreach (TopicUnlock unlock in topicEntry.unlocks)
        {
            if(unlock.topic == null) continue;
            if(unlock.scope == UnlockScope.Conversation)
            {
                if(!unlockedThisConversation.Contains(unlock.topic)) unlockedThisConversation.Add(unlock.topic);
            }
            else
            {
                GameSaveManager.LearnTopic(unlock.topic, unlock.scope);
            }
        }
        AddToLog(topicEntry.Label, topicEntry.response, topicEntry.topic);
        RebuildTopicList();
    }

    private void AddToLog(string heading, string text, Topic topic = null)
    {
        if(string.IsNullOrEmpty(text)) return;
        log.Add(new LogEntry {heading = heading, text = text, topic = topic});

        if(log.Count > 76)
        {
            log.RemoveRange(0, log.Count - 76);
        }

        RedrawLog();
    }

    // Rebuilds the whole log, linking topic mentions in the NPC's text. Redrawn rather than appended to,
    // so older text picks up links to topics unlocked since it was said.
    private void RedrawLog()
    {
        StringBuilder builder = new StringBuilder();
        string headingHex = ColorUtility.ToHtmlStringRGB(topicHeadingColor);

        Dictionary<string, TopicEntry> keywords = KeywordsFor(AskableTopics());
        Regex keywordPattern = KeywordPattern(keywords.Keys);

        foreach (LogEntry entry in log)
        {
            if(builder.Length > 0) builder.Append("\n\n");
            if(entry.heading != null) builder.Append($"<b><color=#{headingHex}>{entry.heading}</color></b>\n");
            builder.Append(AddLinks(entry.text, entry.topic, keywords, keywordPattern));
        }

        logText.text = builder.ToString();
        ScrollToBottom();
    }

    // [shown text](Topic): the shown text links to that topic
    private static readonly Regex explicitLinkPattern = new Regex(@"\[(.+?)\]\((.+?)\)");

    // Turns topic mentions in one piece of NPC text into clickable links. ownTopic is the topic this text answers:
    // automatic links skip it, so "Village"'s answer doesn't link "village".
    private string AddLinks(string text, Topic ownTopic, Dictionary<string, TopicEntry> keywords, Regex keywordPattern)
    {
        StringBuilder result = new StringBuilder();
        int plainStart = 0;

        foreach (Match match in explicitLinkPattern.Matches(text))
        {
            // The ordinary text before this [shown](Topic) gets automatic links...
            result.Append(AutoLink(text.Substring(plainStart, match.Index - plainStart), ownTopic, keywords, keywordPattern));
            // ...and the [shown](Topic) itself becomes exactly the link it asks for
            result.Append(ExplicitLink(match.Groups[1].Value, match.Groups[2].Value));
            plainStart = match.Index + match.Length;
        }
        result.Append(AutoLink(text.Substring(plainStart), ownTopic, keywords, keywordPattern));
        return result.ToString();
    }

    private string ExplicitLink(string shownText, string topicName)
    {
        TopicEntry entry = FindEntry(topicName);
        if (entry == null)
        {
            Debug.LogWarning($"{dialogue.name}: [{shownText}]({topicName}) doesn't match any of this NPC's topics");
            return shownText;
        }
        return CanAsk(entry) ? LinkTo(entry, shownText) : shownText;
    }

    private string AutoLink(string text, Topic ownTopic, Dictionary<string, TopicEntry> keywords, Regex keywordPattern)
    {
        if (keywordPattern == null) return text;
        return keywordPattern.Replace(text, match =>
        {
            TopicEntry entry = keywords[match.Value];
            return entry.topic == ownTopic ? match.Value : LinkTo(entry, match.Value);
        });
    }

    // Every word or phrase that should link to an askable topic: its name and this NPC's alias for it.
    // Case-insensitive, so "the loop" in the text finds the keyword "The Loop".
    private static Dictionary<string, TopicEntry> KeywordsFor(List<TopicEntry> askable)
    {
        var keywords = new Dictionary<string, TopicEntry>(System.StringComparer.OrdinalIgnoreCase);
        foreach (TopicEntry entry in askable)
        {
            keywords.TryAdd(entry.Label, entry);
            keywords.TryAdd(entry.topic.DisplayName, entry);
        }
        return keywords;
    }

    // One pattern matching any keyword as a whole word or phrase, longest first,
    // so "time loop" is linked as one phrase rather than just its "loop"
    private static Regex KeywordPattern(IEnumerable<string> keywords)
    {
        string[] longestFirst = keywords.OrderByDescending(k => k.Length).Select(Regex.Escape).ToArray();
        if (longestFirst.Length == 0) return null;
        return new Regex($@"(?<!\w)(?:{string.Join("|", longestFirst)})(?!\w)", RegexOptions.IgnoreCase);
    }

    private string LinkTo(TopicEntry entry, string shownText)
    {
        string linkHex = ColorUtility.ToHtmlStringRGB(linkColor);
        return $"<link=\"{entry.topic.name}\"><color=#{linkHex}>{shownText}</color></link>";
    }

    // Finds this NPC's entry for a topic by asset name or display name, ignoring case
    private TopicEntry FindEntry(string topicName)
    {
        return dialogue.AllTopics.FirstOrDefault(entry => entry.topic != null &&
            (string.Equals(entry.topic.name, topicName, System.StringComparison.OrdinalIgnoreCase) ||
             string.Equals(entry.topic.DisplayName, topicName, System.StringComparison.OrdinalIgnoreCase)));
    }

    // ScreenManager sends every click here; if it landed on a link in the log, ask about that topic
    public override void OnClick()
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(logText, Mouse.current.position.ReadValue(), null);
        if (linkIndex < 0) return;

        string topicName = logText.textInfo.linkInfo[linkIndex].GetLinkID();
        TopicEntry entry = FindEntry(topicName);
        if (entry != null && CanAsk(entry)) Ask(entry);
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        logScroll.verticalNormalizedPosition = 0f;
    }

    public override void OnCancel()
    {
        Close();
    }



}
