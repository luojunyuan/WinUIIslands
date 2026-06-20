using System.Runtime.InteropServices;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using WinRT;

namespace CoreIsland;

public abstract class SystemBackdrop
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private UISettings? _uiSettings;
    private Window? _window;
    private HWND _hwnd;

    internal void Apply(Window window)
    {
        _window = window;
        _hwnd = window.Hwnd;
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
        SetXamlBackgroundTransparency(true);
        RefreshTheme(window);
    }

    internal void Remove(Window window)
    {
        _uiSettings?.ColorValuesChanged -= OnColorValuesChanged;
        _uiSettings = null;
        _window = null;
        OnRemove(window);
        SetXamlBackgroundTransparency(false);
    }

    internal void RefreshTheme(Window window)
    {
        _hwnd = window.Hwnd;
        ApplyDarkMode(_hwnd);
        OnApply(window);
    }

    protected abstract void OnApply(Window window);
    protected abstract void OnRemove(Window window);

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (_window is not null)
                RefreshTheme(_window);
        });
    }

    private static void ApplyDarkMode(HWND hwnd)
    {
        var isDark = Application.Current.RequestedTheme == Windows.UI.Xaml.ApplicationTheme.Dark ? 1 : 0;
        var darkSpan = MemoryMarshal.CreateSpan(ref isDark, 1);
        _ = PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, MemoryMarshal.AsBytes(darkSpan));
    }

    private static void SetXamlBackgroundTransparency(bool enabled)
    {
        try
        {
            global::Windows.UI.Xaml.Window.Current.As<IXamlSourceTransparency>().IsBackgroundTransparent = enabled;
        }
        catch
        {
        }
    }
}
