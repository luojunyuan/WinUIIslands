using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIIslands.Windowing;

public sealed class AppWindow
{
    private static readonly Dictionary<nint, AppWindow> s_registry = [];

    private readonly OverlappedPresenter _presenter;

    internal HWND Hwnd { get; }

    public AppWindowPresenter Presenter => _presenter;

    private AppWindow(HWND hwnd)
    {
        Hwnd = hwnd;
        _presenter = new OverlappedPresenter(this);
    }

    public static AppWindow GetFromWindowId(WindowId windowId)
    {
        var key = (nint)windowId.Value;
        if (s_registry.TryGetValue(key, out var existing))
            return existing;

        var appWindow = new AppWindow(new HWND(key));
        s_registry[key] = appWindow;
        return appWindow;
    }

    public void Resize(SizeInt32 clientSize)
    {
        uint dpi = PInvoke.GetDpiForWindow(Hwnd);
        var style = (WINDOW_STYLE)(nint)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var exStyle = (WINDOW_EX_STYLE)(nint)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);

        RECT rect = new() { right = clientSize.Width, bottom = clientSize.Height };
        PInvoke.AdjustWindowRectExForDpi(ref rect, style, false, exStyle, dpi);

        PInvoke.SetWindowPos(Hwnd, default, 0, 0, rect.Width, rect.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
    }
}
