using System.Runtime.InteropServices;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace CoreIsland;

public abstract class SystemBackdrop
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private UISettings? _uiSettings;
    private HWND _hwnd;

    internal void Apply(Window window)
    {
        _hwnd = window.Hwnd;
        ApplyDarkMode(_hwnd);
        _uiSettings = new UISettings();
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
        OnApply(window);
    }

    internal void Remove(Window window)
    {
        _uiSettings?.ColorValuesChanged -= OnColorValuesChanged;
        _uiSettings = null;
        OnRemove(window);
    }

    protected abstract void OnApply(Window window);
    protected abstract void OnRemove(Window window);

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        _dispatcherQueue.TryEnqueue(() => ApplyDarkMode(_hwnd));
    }

    private static void ApplyDarkMode(HWND hwnd)
    {
        var isDark = Application.Current.RequestedTheme == Windows.UI.Xaml.ApplicationTheme.Dark ? 1 : 0;
        var darkSpan = MemoryMarshal.CreateSpan(ref isDark, 1);
        _ = PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, MemoryMarshal.AsBytes(darkSpan));
    }
}
