using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;

public partial class Application : Windows.UI.Xaml.Application
{
    internal static HWND CoreHwnd;
    public new static Application Current { get; private set; } = null!;

    private readonly WindowsXamlManager _xamlManager;

    protected Application()
    {
        if (Current != null) throw new InvalidOperationException("An instance of Application has already been created.");
        Current = this;

        // Note: this will immediatly call OnLaunched
        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    protected sealed override void OnLaunched(LaunchActivatedEventArgs e)
    {
        global::Windows.UI.Core.CoreWindow.GetForCurrentThread().HideWindowInWin10(out CoreHwnd);

        var xamlWindowBoundToCoreWindow = global::Windows.UI.Xaml.Window.Current;
        xamlWindowBoundToCoreWindow.As<IXamlSourceTransparency>().IsBackgroundTransparent = true;

        // Ensure OnIslandLaunched is calling after ctor of derived class is completed
        var queue = DispatcherQueue.GetForCurrentThread();
        queue.TryEnqueue(() =>
        {
            OnIslandLaunched(e);
        });
    }

    protected virtual void OnIslandLaunched(LaunchActivatedEventArgs e) { }

    public int Run()
    {
        MSG msg;
        while (PInvoke.GetMessage(out msg, default, 0, 0).Value > 0)
        {
            PInvoke.TranslateMessage(in msg);
            PInvoke.DispatchMessage(in msg);
        }

        return (int)msg.wParam.Value;
    }

    public int Run(Window window)
    {
        window.Activate();
        return Run();
    }
}
