using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public partial class Window
{
    public void Activate()
    {
        Loading?.Invoke(this, EventArgs.Empty);
        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(_hwnd);
        Loaded?.Invoke(this, default);
    }

    public void Close() => PInvoke.SendMessage(_hwnd, PInvoke.WM_CLOSE, 0, 0);
}
