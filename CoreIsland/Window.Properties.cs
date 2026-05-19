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

    public new event EventHandler<WindowSizeChangedEventArgs>? SizeChanged;

    private void OnSizeChanged(int width, int height)
    {
        SizeChanged?.Invoke(this, new WindowSizeChangedEventArgs(width, height));
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

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }
}
