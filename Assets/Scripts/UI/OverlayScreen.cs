using UnityEngine;

public abstract class OverlayScreen : MonoBehaviour
{
    public bool IsClosed {get; private set;}

    public virtual void OnClick() {}

    public virtual void OnCancel() {}

    protected void Close()
    {
        if (IsClosed) return;
        IsClosed = true;
        ScreenManager.Instance.CloseScreen(this);
        Destroy(gameObject);
    }
}
