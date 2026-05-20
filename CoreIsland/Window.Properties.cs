using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.Win32;

namespace CoreIsland;

public sealed class WindowSizeChangedEventArgs : EventArgs
{
    public int Width { get; }
    public int Height { get; }

    internal WindowSizeChangedEventArgs(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

[ContentProperty(Name = nameof(Content))]
public unsafe partial class Window : FrameworkElement
{
    public new event TypedEventHandler<FrameworkElement, object>? Loading;

    public new event RoutedEventHandler? Loaded;

    public event EventHandler<WindowSizeChangedEventArgs>? WindowSizeChanged;

    private void OnSizeChanged(int width, int height)
    {
        WindowSizeChanged?.Invoke(this, new WindowSizeChangedEventArgs(width, height));
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(Window),
        new PropertyMetadata(string.Empty, (d, e) =>
        {
            var window = (Window)d;
            var value = (string)e.NewValue;
            if (window._hwnd != default)
                PInvoke.SetWindowText(window._hwnd, value);
        }));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SystemBackdropProperty = DependencyProperty.Register(
        nameof(SystemBackdrop),
        typeof(SystemBackdrop),
        typeof(Window),
        new PropertyMetadata(null, (d, e) =>
        {
            var window = (Window)d;
            if (e.OldValue is SystemBackdrop oldBackdrop)
                oldBackdrop.Remove(window);
            if (e.NewValue is SystemBackdrop newBackdrop)
                newBackdrop.Apply(window);
        }));

    public SystemBackdrop? SystemBackdrop
    {
        get => (SystemBackdrop?)GetValue(SystemBackdropProperty);
        set => SetValue(SystemBackdropProperty, value);
    }

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }
}
