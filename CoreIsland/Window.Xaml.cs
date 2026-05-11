using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.Win32;

namespace CoreIsland;

[ContentProperty(Name = nameof(Content))]
public unsafe partial class Window
{
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