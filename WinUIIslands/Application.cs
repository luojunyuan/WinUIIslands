using Windows.ApplicationModel.Activation;
using Windows.System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIIslands;

public partial class Application : Windows.UI.Xaml.Application
{
    internal static HWND CoreHwnd;
    public new static Application Current { get; private set; } = null!;

    private readonly WindowsXamlManager _xamlManager;
    private UISettings? _uiSettings;
    private DispatcherQueue? _dispatcherQueue;
    private global::Windows.UI.Xaml.ApplicationTheme _requestedTheme = global::Windows.UI.Xaml.ApplicationTheme.Light;
    private bool _isRequestedThemeExplicitlySet;

    protected Application()
    {
        if (Current != null) throw new InvalidOperationException("An instance of Application has already been created.");
        Current = this;

        // Note: this will immediatly call OnLaunched
        _xamlManager = WindowsXamlManager.InitializeForCurrentThread();
    }

    public new global::Windows.UI.Xaml.ApplicationTheme RequestedTheme
    {
        get => _requestedTheme;
        set
        {
            _isRequestedThemeExplicitlySet = true;
            SetRequestedTheme(value);
        }
    }

    protected sealed override void OnLaunched(LaunchActivatedEventArgs e)
    {
        global::Windows.UI.Core.CoreWindow.GetForCurrentThread()
            .HideWindowInWin10(out CoreHwnd);

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _dispatcherQueue = dispatcherQueue;
        SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(dispatcherQueue));
        ApplySystemRequestedTheme();
        SubscribeSystemThemeChanges();

        // Ensure OnIslandLaunched is calling after ctor of derived class is completed
        dispatcherQueue.TryEnqueue(() =>
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

    private void SetRequestedTheme(global::Windows.UI.Xaml.ApplicationTheme theme)
    {
        if (_requestedTheme == theme)
            return;

        _requestedTheme = theme;
        OnRequestedThemeChanged();
    }

    private void ApplySystemRequestedTheme()
    {
        if (!_isRequestedThemeExplicitlySet)
            SetRequestedTheme(GetSystemRequestedTheme());
    }

    private void SubscribeSystemThemeChanges()
    {
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        if (_isRequestedThemeExplicitlySet)
            return;

        _dispatcherQueue?.TryEnqueue(ApplySystemRequestedTheme);
    }

    private static global::Windows.UI.Xaml.ApplicationTheme GetSystemRequestedTheme()
    {
        try
        {
            var foreground = new UISettings().GetColorValue(UIColorType.Foreground);
            return IsColorLight(foreground)
                ? global::Windows.UI.Xaml.ApplicationTheme.Dark
                : global::Windows.UI.Xaml.ApplicationTheme.Light;
        }
        catch
        {
            return global::Windows.UI.Xaml.ApplicationTheme.Light;
        }
    }

    private static bool IsColorLight(Color color) => 5 * color.G + 2 * color.R + color.B > 8 * 128;
}
