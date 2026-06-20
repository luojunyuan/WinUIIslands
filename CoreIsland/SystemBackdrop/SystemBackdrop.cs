using Windows.System;
using Windows.UI.ViewManagement;
using WinRT;

namespace CoreIsland;

public abstract class SystemBackdrop
{
    private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    private UISettings? _uiSettings;
    private Window? _window;

    internal void Apply(Window window)
    {
        _window = window;
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
