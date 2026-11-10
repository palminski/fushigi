using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
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

    [Header("Topics")]
    [SerializeField] private Transform topicList;
    [SerializeField] private Button topicButtonPrefab;
    [SerializeField] private Button goodbyeButton;

    private class LogEntry
    {
        public string heading;
        public string text;
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

    private void Ask(TopicEntry topicEntry)
    {
        AddToLog(topicEntry.Label, topicEntry.response);

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
        RebuildTopicList();
    }

    private void AddToLog(string heading, string text)
    {
        if(string.IsNullOrEmpty(text)) return;
        log.Add(new LogEntry {heading = heading, text = text});

        if(log.Count > 76)
        {
            log.RemoveRange(0, log.Count - 76);
        }

        RedrawLog();
    }

    private void RedrawLog()
    {
        StringBuilder builder = new StringBuilder();
        string headingHex = ColorUtility.ToHtmlStringRGB(topicHeadingColor);

        foreach (LogEntry entry in log)
        {
            if(builder.Length > 0) builder.Append("\n\n");
            if(entry.heading != null) builder.Append($"<b><color=#{headingHex}>{entry.heading}</color></b>\n");
            builder.Append(entry.text);
        }

        logText.text = builder.ToString();
        ScrollToBottom();
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
