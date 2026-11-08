using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance {get; private set;}

    public bool IsBlocking {get; private set;}

    void Awake()
    {
        if (Instance != null && Instance != this) {Destroy(gameObject); return;}
        Instance = this;
    }

    public event System.Action ScreenUp;
    public event System.Action ScreenGone;

    public void Open()
    {
        if(IsBlocking) return;
        IsBlocking = true;
        ScreenUp?.Invoke();
    }

    public void Close()
    {
        if(!IsBlocking) return;
        IsBlocking = false;
        ScreenGone?.Invoke();
    }
}
