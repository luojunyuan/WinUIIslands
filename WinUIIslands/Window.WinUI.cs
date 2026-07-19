using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIIslands;

public partial class Window
{
    public void Activate()
    {
        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(_hwnd);
    }

    public void Close() => PInvoke.SendMessage(_hwnd, PInvoke.WM_CLOSE, 0, 0);
}
