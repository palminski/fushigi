using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class MapObjectDialogue : MonoBehaviour
{
    [SerializeField] private Conversation conversation;
    [SerializeField] private NpcDialogue npcDialogue;

    [SerializeField] private List<PlayerUnit> unitsWillSpeakTo;
    [SerializeField] private List<StoryFlag> requiredFlags;
    [SerializeField] private List<StoryFlag> forbiddenFlags;

    [SerializeField] private bool shouldDestroyClassInstanceUponDialogueCompletion = true;
    


    public void InitiateInteraction(PlayerUnit playerUnit)
    {
        if (conversation != null || npcDialogue != null) StartCoroutine(StartInteractionCoroutine(playerUnit));
    }

    private IEnumerator StartInteractionCoroutine(PlayerUnit initiatingUnit)
    {
        yield return InitiateConversation();
        yield return InitiateDialogue(initiatingUnit);

        if (shouldDestroyClassInstanceUponDialogueCompletion)
        {
            Destroy(this);
            enabled = false;
        }
        initiatingUnit.mover.LockMove();
        InputController.Instance.OpenActionMenu(initiatingUnit);
    }
    private IEnumerator InitiateConversation()
    {
        yield return DialogueManager.Instance.PlayConversation(conversation);
    }
    private IEnumerator InitiateDialogue(PlayerUnit initiatingUnit)
    {
        yield return DialogueManager.Instance.StartTopicDialogue(npcDialogue, initiatingUnit);
    }

    public bool CanBeSpokenTo(PlayerUnit initiatingUnit)
    {
        if (!enabled) return false;
        
        if (conversation == null && npcDialogue == null) return false;

        if (unitsWillSpeakTo.Count > 0 && !unitsWillSpeakTo.Contains(initiatingUnit)) return false;
        
        foreach (StoryFlag flag in requiredFlags)
        {
            if (!GameSaveManager.HasFlag(flag)) return false;
        }

        foreach (StoryFlag flag in forbiddenFlags)
        {
            if (GameSaveManager.HasFlag(flag)) return false;
        }
        
        return true;
    }
}
