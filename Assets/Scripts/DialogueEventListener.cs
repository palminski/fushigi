using UnityEngine;
using UnityEngine.Events;

public class DialogueEventListener : MonoBehaviour
{
    [SerializeField]private DialogueEvent listenFor;
    [SerializeField]private UnityEvent onRaised;

    void OnEnable()
    {
        DialogueEvent.Raised += Handle;
    }

    void OnDisable()
    {
        DialogueEvent.Raised -= Handle;
    }

    void Handle(DialogueEvent raised)
    {
        if (raised == listenFor) onRaised.Invoke();
    }
}
