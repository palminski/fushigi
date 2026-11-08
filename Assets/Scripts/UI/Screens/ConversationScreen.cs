using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    public void Play(Conversation conversation)
    {
        this.conversation = conversation;
        lineIndex = 0;
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
        bool hasSpeaker = line.speaker != null;

        namePlate.SetActive(hasSpeaker);
        if (hasSpeaker)
        {
            nameText.text = line.speaker.displayName;

            Image portrait = line.side == PortraitSide.Left ? leftPortrait : rightPortrait;
            portrait.sprite = line.speaker.portrait;
            portrait.enabled = line.speaker.portrait != null;
        }
        lineText.text = line.text;

        SetSpeaking(leftPortrait, hasSpeaker && line.side == PortraitSide.Left);
        SetSpeaking(rightPortrait, hasSpeaker && line.side == PortraitSide.Right);

        typingCoroutine = StartCoroutine(TypeLine());

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
