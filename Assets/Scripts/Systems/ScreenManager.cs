using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance {get; private set;}
    public event System.Action ScreenUp;
    public event System.Action ScreenGone;
    [SerializeField]private Transform screenParent;
    private readonly List<OverlayScreen> openScreens = new List<OverlayScreen>();
    private int lastOpenedFrame = -1;
    private GameInput inputActions;
    
    public bool IsBlocking => openScreens.Count > 0;
    private OverlayScreen TopScreen => openScreens.Count>0 ? openScreens[openScreens.Count - 1] : null;

    void Awake()
    {
        if (Instance != null && Instance != this) {Destroy(gameObject); return;}
        Instance = this;
        inputActions = new GameInput();
    }
    void OnEnable()
    {
        inputActions.Gameplay.Enable();
        inputActions.Gameplay.Click.performed += OnClick;
        inputActions.Gameplay.RightClick.performed += OnRightClick;
    }
    void OnDisable()
    {
        inputActions.Gameplay.Click.performed -= OnClick;
        inputActions.Gameplay.RightClick.performed -= OnRightClick;
        inputActions.Gameplay.Disable();
    }
    public T Open<T>(T prefab) where T : OverlayScreen
    {
        T screen = Instantiate(prefab, screenParent);
        openScreens.Add(screen);
        lastOpenedFrame = Time.frameCount;
        if(openScreens.Count == 1) ScreenUp?.Invoke();
        return screen;
    }

    public IEnumerator OpenAndWait<T>(T prefab, System.Action<T> setup = null) where T : OverlayScreen
    {
        T screen = Open(prefab);
        setup?.Invoke(screen);
        yield return new WaitUntil(() => screen.IsClosed);
    }

    public void CloseScreen(OverlayScreen screen)
    {
        if(!openScreens.Remove(screen)) return;
        if(openScreens.Count == 0) ScreenGone?.Invoke();
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        // Ignore the click that opened the screen (e.g. clicking an enemy to attack it), so it isn't skipped instantly
        if (TopScreen == null || Time.frameCount == lastOpenedFrame) return;
        TopScreen.OnClick();
    }

    private void OnRightClick(InputAction.CallbackContext context)
    {
        if (TopScreen == null || Time.frameCount == lastOpenedFrame) return;
        TopScreen.OnCancel();
    }

}
