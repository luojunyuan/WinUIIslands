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

        Activated += MainWindow_Activated;
        SizeChanged += MainWindow_SizeChanged;
        WindowingTitleBar.SizeChanged += WindowingTitleBar_SizeChanged;
        WindowingTitleBar.LayoutUpdated += WindowingTitleBar_LayoutUpdated;

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

            WindowingTitleBar.RecomputeDragRegions();
            RefreshTitleBar();
            return;
        }

        _titleBarAdapter.WindowTitleBar.ExtendsContentIntoTitleBar = false;
        _titleBarAdapter.SetCaptionInsets(0, 0);

        SetTitleBar(null);
        ExtendsContentIntoTitleBar = false;
    }

    private void MainWindow_Activated(object sender, CoreIsland.WindowActivatedEventArgs e)
    {
        _titleBarAdapter.NotifyWindowActivated(e.IsActive);
    }

    private void MainWindow_SizeChanged(object sender, CoreIsland.WindowSizeChangedEventArgs e)
    {
        RefreshTitleBar();
    }

    private void WindowingTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RecomputeTitleBarRegions();
    }

    private void WindowingTitleBar_LayoutUpdated(object? sender, object e)
    {
        RecomputeTitleBarRegions();
    }

    private void RecomputeTitleBarRegions()
    {
        if (!IsCustomTitleBarEnabled)
            return;

        WindowingTitleBar.RecomputeDragRegions();
        RefreshTitleBar();
    }
}
