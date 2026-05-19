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

    private string _title = "";

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_hwnd != default)
                PInvoke.SetWindowText(_hwnd, value);
        }
    }

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }
}
