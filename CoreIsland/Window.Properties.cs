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

public sealed class WindowEventArgs : EventArgs;

[ContentProperty(Name = nameof(Content))]
public partial class Window
{
    public event TypedEventHandler<object, WindowEventArgs>? Closed;

    public event TypedEventHandler<object, WindowSizeChangedEventArgs>? SizeChanged;

    private void OnSizeChanged(int width, int height)
    {
        SizeChanged?.Invoke(this, new WindowSizeChangedEventArgs(width, height));
    }

    private void OnClosed()
    {
        Closed?.Invoke(this, new WindowEventArgs());
    }

    public string Title
    {
        get => field;
        set { field = value; PInvoke.SetWindowText(_hwnd, value); }
    } = string.Empty;

    public SystemBackdrop? SystemBackdrop
    {
        get => field;
        set
        {
            field?.Remove(this);
            field = value;
            value?.Apply(this);
        }
    }

    public UIElement Content
    {
        get => _xamlHost.Content;
        set => _xamlHost.Content = value;
    }
}
