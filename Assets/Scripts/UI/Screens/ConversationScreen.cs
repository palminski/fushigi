using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ConversationScreen : OverlayScreen
{
    [SerializeField]private Image leftPortrait;
    [SerializeField]private Image rightPortrait;
    [SerializeField]private GameObject namePlate;
    [SerializeField]private TMP_Text nameText;
    [SerializeField]private TMP_Text lineText;
    [SerializeField]private Color listeningTint = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private float charactersPerSecond = 40f;

    private Coroutine typingCoroutine;

    private Conversation conversation;
    private int lineIndex;

    // Who is currently standing on each side - lines only change a side when they say so
    private Speaker leftSpeaker;
    private Speaker rightSpeaker;

    public void Play(Conversation conversation)
    {
        this.conversation = conversation;
        lineIndex = 0;
        leftSpeaker = null;
        rightSpeaker = null;
        leftPortrait.enabled = false;
        rightPortrait.enabled = false;

        if (conversation == null || conversation.lines.Count == 0)
        {
            Close();
            return;
        }
        ShowLine(conversation.lines[lineIndex]);
    }

    private void ShowLine(DialogueLine line)
    {
        leftSpeaker = ApplySlot(leftPortrait, line.left, leftSpeaker);
        rightSpeaker = ApplySlot(rightPortrait, line.right, rightSpeaker);

        // The name plate names whoever is lit: one name, "A & B" if both are, hidden for narration
        List<string> names = new List<string>();
        if (line.left.IsLit) names.Add(leftSpeaker.displayName);
        if (line.right.IsLit) names.Add(rightSpeaker.displayName);
        namePlate.SetActive(names.Count > 0);
        nameText.text = string.Join(" & ", names);

        lineText.text = line.text;
        typingCoroutine = StartCoroutine(TypeLine());
    }

    // Updates one side from the line's slot and returns who is standing there afterwards
    private Speaker ApplySlot(Image portrait, PortraitSlot slot, Speaker current)
    {
        if (slot.clear) current = null;
        else if (slot.speaker != null) current = slot.speaker;

        portrait.sprite = current != null ? current.portrait : null;
        portrait.enabled = current != null && current.portrait != null;
        SetSpeaking(portrait, slot.IsLit);
        return current;
    }

    private void SetSpeaking(Image portrait, bool isSpeaking)
    {
        portrait.color = isSpeaking ? Color.white : listeningTint;
    }

    public override void OnClick()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            lineText.maxVisibleCharacters = lineText.textInfo.characterCount;
            return;
        }
        NextLine();
    }

    public override void OnCancel()
    {
        Close();
    }

    private void NextLine()
    {
        lineIndex++;
        if(lineIndex >= conversation.lines.Count)
        {
            Close();
            return;
        }
        ShowLine(conversation.lines[lineIndex]);
    }

    private IEnumerator TypeLine()
    {
        lineText.maxVisibleCharacters = 0;
        lineText.ForceMeshUpdate();
        int letterCount = lineText.textInfo.characterCount;

        for (float shown = 0; shown < letterCount; shown += charactersPerSecond * Time.deltaTime)
        {
            lineText.maxVisibleCharacters = (int)shown;
            yield return null;
        }

        lineText.maxVisibleCharacters = letterCount;
        typingCoroutine = null;
    }
}
