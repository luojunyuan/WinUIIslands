using CoreIsland.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public unsafe partial class Window
{
    private AppWindow? _appWindow;

    public AppWindow? AppWindow
    {
        get
        {
            if (_appWindow is null && _hwnd != default)
            {
                var windowId = new WindowId((uint)_hwnd.Value);
                _appWindow = AppWindow.GetFromWindowId(windowId);
            }
            return _appWindow;
        }
    }

    private int ScaleToPhysicalPixels(int logicalPixels)
    {
        uint dpi = PInvoke.GetDpiForWindow(_xamlHwnd);
        return (int)(logicalPixels * dpi / PInvoke.USER_DEFAULT_SCREEN_DPI);
    }

    private void HandleGetMinMaxInfo(LPARAM lParam)
    {
        if (_appWindow?.Presenter is OverlappedPresenter presenter)
        {
            var mmi = (MINMAXINFO*)lParam.Value;
            if (presenter.PreferredMinimumWidth > 0)
                mmi->ptMinTrackSize.X = ScaleToPhysicalPixels(presenter.PreferredMinimumWidth);
            if (presenter.PreferredMinimumHeight > 0)
                mmi->ptMinTrackSize.Y = ScaleToPhysicalPixels(presenter.PreferredMinimumHeight);
        }
    }
}
