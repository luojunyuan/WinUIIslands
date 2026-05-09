using Windows.Win32;

namespace CoreIsland;

public partial class Application
{
    private readonly List<Window> _windows = [];
    public IReadOnlyList<Window> Windows => _windows;

    public Window? MainWindow { get; set; }

    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

    internal Window? CoreOwner { get; private set; }

    internal void RegisterWindow(Window window)
    {
        _windows.Add(window);
        MainWindow ??= window;
    }

    internal void OnWindowActivated(Window window)
    {
        if (CoreOwner != window)
        {
            PInvoke.SetParent(CoreHwnd, window.Hwnd);
            CoreOwner = window;
        }
    }

    internal void OnWindowClosing(Window window)
    {
        _windows.Remove(window);
        if (CoreOwner == window && _windows.Count > 0)
        {
            var next = _windows[^1];
            PInvoke.SetParent(CoreHwnd, next.Hwnd);
            CoreOwner = next;
        }

        if (MainWindow == window)
        {
            MainWindow = null;
            if (ShutdownMode == ShutdownMode.OnMainWindowClose)
            {
                PInvoke.PostQuitMessage(0);
                return;
            }
        }

        if (_windows.Count == 0 && ShutdownMode != ShutdownMode.OnExplicitShutdown)
            PInvoke.PostQuitMessage(0);
    }

    public void Shutdown(int exitCode = 0) => PInvoke.PostQuitMessage(exitCode);
}

public enum ShutdownMode
{
    OnLastWindowClose,
    OnMainWindowClose,
    OnExplicitShutdown,
}