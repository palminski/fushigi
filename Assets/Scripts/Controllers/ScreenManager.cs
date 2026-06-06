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

    public void Open() => IsBlocking = true;
    public void Close() => IsBlocking = false;
}
