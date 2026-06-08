using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CoreIsland.TitleBar;

public enum TitleBarDisplayMode
{
    Standard,
    Tall,
}

public sealed partial class TitleBarControl : UserControl
{
    private Window? _configuredWindow;

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(TitleBarControl), new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(TitleBarControl), new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(TitleBarControl), new PropertyMetadata(string.Empty, OnVisualPropertyChanged));

    public static readonly DependencyProperty TitleBarContentProperty =
        DependencyProperty.Register(nameof(TitleBarContent), typeof(object), typeof(TitleBarControl), new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty FooterProperty =
        DependencyProperty.Register(nameof(Footer), typeof(object), typeof(TitleBarControl), new PropertyMetadata(null, OnVisualPropertyChanged));

    public static readonly DependencyProperty DisplayModeProperty =
        DependencyProperty.Register(nameof(DisplayMode), typeof(TitleBarDisplayMode), typeof(TitleBarControl), new PropertyMetadata(TitleBarDisplayMode.Standard, OnVisualPropertyChanged));

    public static readonly DependencyProperty AutoConfigureCustomTitleBarProperty =
        DependencyProperty.Register(nameof(AutoConfigureCustomTitleBar), typeof(bool), typeof(TitleBarControl), new PropertyMetadata(true, OnAutoConfigureCustomTitleBarChanged));

    public static readonly DependencyProperty WindowProperty =
        DependencyProperty.Register(nameof(Window), typeof(Window), typeof(TitleBarControl), new PropertyMetadata(null, OnWindowChanged));

    public TitleBarControl()
    {
        InitializeComponent();
        Loaded += TitleBarControl_Loaded;
        Unloaded += TitleBarControl_Unloaded;
        SizeChanged += TitleBarControl_SizeChanged;
        Update();
    }

    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? TitleBarContent
    {
        get => GetValue(TitleBarContentProperty);
        set => SetValue(TitleBarContentProperty, value);
    }

    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    public TitleBarDisplayMode DisplayMode
    {
        get => (TitleBarDisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public bool AutoConfigureCustomTitleBar
    {
        get => (bool)GetValue(AutoConfigureCustomTitleBarProperty);
        set => SetValue(AutoConfigureCustomTitleBarProperty, value);
    }

    public Window? Window
    {
        get => (Window?)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    private void TitleBarControl_Loaded(object sender, RoutedEventArgs e) => Configure();

    private void TitleBarControl_Unloaded(object sender, RoutedEventArgs e) => Reset();

    private void TitleBarControl_SizeChanged(object sender, SizeChangedEventArgs e) => Configure();

    private void Configure()
    {
        if (!AutoConfigureCustomTitleBar || Window is null)
            return;

        if (_configuredWindow is not null && !ReferenceEquals(_configuredWindow, Window))
            DetachWindow(_configuredWindow);

        if (_configuredWindow is null)
        {
            _configuredWindow = Window;
            _configuredWindow.Activated += ConfiguredWindow_Activated;
            _configuredWindow.SizeChanged += ConfiguredWindow_SizeChanged;
        }

        Window.ExtendsContentIntoTitleBar = true;
        Window.SetTitleBar(DragRegion);
        CaptionButtons.Window = Window;
        UpdateWindowState();
    }

    public void Reset()
    {
        if (_configuredWindow is not null)
            DetachWindow(_configuredWindow);

        CaptionButtons.Window = null;
    }

    private void Update()
    {
        IconPresenterHost.Visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        SubtitleTextBlock.Visibility = string.IsNullOrEmpty(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        ContentPresenter.Visibility = TitleBarContent is null ? Visibility.Collapsed : Visibility.Visible;
        FooterPresenter.Visibility = Footer is null ? Visibility.Collapsed : Visibility.Visible;
        VisualStateManager.GoToState(this, DisplayMode == TitleBarDisplayMode.Tall ? "Tall" : "Standard", true);

        UpdateWindowState();
    }

    private void ConfiguredWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        UpdateWindowState();
    }

    private void ConfiguredWindow_Activated(object sender, WindowActivatedEventArgs e)
    {
        IsWindowActive(e.IsActive);
    }

    private void UpdateWindowState()
    {
        if (Window is not null)
            CaptionButtons.IsWindowMaximized(Windows.Win32.PInvoke.IsZoomed(new Windows.Win32.Foundation.HWND(CoreIsland.Windowing.WindowNative.GetWindowHandle(Window))));
    }

    private void IsWindowActive(bool value)
    {
        VisualStateManager.GoToState(this, value ? "Active" : "NotActive", true);
    }

    private void DetachWindow(Window window)
    {
        window.Activated -= ConfiguredWindow_Activated;
        window.SizeChanged -= ConfiguredWindow_SizeChanged;
        window.SetTitleBar(null);
        window.ExtendsContentIntoTitleBar = false;
        _configuredWindow = null;
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TitleBarControl)d).Update();
    }

    private static void OnAutoConfigureCustomTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var titleBar = (TitleBarControl)d;
        if ((bool)e.NewValue)
            titleBar.Configure();
        else
            titleBar.Reset();
    }

    private static void OnWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var titleBar = (TitleBarControl)d;
        if (e.OldValue is Window oldWindow)
            titleBar.DetachWindow(oldWindow);

        titleBar.Configure();
        titleBar.Update();
    }
}
