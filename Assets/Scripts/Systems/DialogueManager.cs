using UnityEngine;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance {get; private set;}
    [SerializeField] private ConversationScreen conversationScreenPrefab;
    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public IEnumerator PlayConversation(Conversation conversation)
    {
        if(conversation == null || conversation.lines.Count == 0) yield break;
        yield return ScreenManager.Instance.OpenAndWait(conversationScreenPrefab, screen => screen.Play(conversation));
    }
}
