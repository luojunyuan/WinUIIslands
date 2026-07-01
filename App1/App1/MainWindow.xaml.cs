using CoreIsland.Windowing;
using Islands.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace App1;

public sealed partial class MainWindow : CoreIsland.Window
{
    private const double CaptionButtonsRightInset = 138;

    private readonly TitleBarWindowAdapter _titleBarAdapter;

    private bool _isCustomTitleBarEnabled = true;

    public MainWindow()
    {
        InitializeComponent();

        _titleBarAdapter = new TitleBarWindowAdapter(WindowingTitleBar)
        {
            WindowHandle = (long)WindowNative.GetWindowHandle(this),
        };

        ApplyCustomTitleBarState();
    }

    public UIElement? PageContent
    {
        get => ContentHost.Content as UIElement;
        set => ContentHost.Content = value;
    }

    public bool IsCustomTitleBarEnabled
    {
        get => _isCustomTitleBarEnabled;
        set
        {
            if (_isCustomTitleBarEnabled == value)
                return;

            _isCustomTitleBarEnabled = value;
            ApplyCustomTitleBarState();
        }
    }

    private void ApplyCustomTitleBarState()
    {
        CustomTitleBarHost.Visibility = IsCustomTitleBarEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (IsCustomTitleBarEnabled)
        {
            _titleBarAdapter.WindowHandle = (long)WindowNative.GetWindowHandle(this);
            _titleBarAdapter.WindowTitleBar.ExtendsContentIntoTitleBar = true;
            _titleBarAdapter.SetCaptionInsets(0, CaptionButtonsRightInset);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(WindowingTitleBar, _titleBarAdapter.HitTest, _titleBarAdapter.ApplyTitleBarWindowRegion);
            return;
        }

        _titleBarAdapter.WindowTitleBar.ExtendsContentIntoTitleBar = false;
        _titleBarAdapter.SetCaptionInsets(0, 0);

        SetTitleBar(null);
        ExtendsContentIntoTitleBar = false;
    }
}
