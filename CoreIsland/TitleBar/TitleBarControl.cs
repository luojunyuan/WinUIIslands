using Microsoft.Win32;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace CoreIsland.TitleBar;

public enum TitleBarDisplayMode
{
    Standard,
    Tall,
}

public sealed partial class TitleBarControl : UserControl
{
    private readonly Grid _rootGrid;
    private readonly Button _backButton;
    private readonly Button _paneToggleButton;
    private readonly Viewbox _iconHost;
    private readonly TextBlock _titleTextBlock;
    private readonly TextBlock _subtitleTextBlock;
    private readonly ContentPresenter _contentPresenter;
    private readonly ContentPresenter _footerPresenter;
    private readonly Border _topBorder;

    private CoreIsland.Window? _configuredWindow;

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
        DependencyProperty.Register(nameof(Window), typeof(CoreIsland.Window), typeof(TitleBarControl), new PropertyMetadata(null, OnWindowChanged));

    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register(nameof(IsBackButtonVisible), typeof(bool), typeof(TitleBarControl), new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsBackButtonEnabledProperty =
        DependencyProperty.Register(nameof(IsBackButtonEnabled), typeof(bool), typeof(TitleBarControl), new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty =
        DependencyProperty.Register(nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(TitleBarControl), new PropertyMetadata(false, OnVisualPropertyChanged));

    public TitleBarControl()
    {
        Height = 40;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Top;
        Background = new SolidColorBrush(Colors.Transparent);

        _rootGrid = CreateRootGrid();
        _topBorder = new Border
        {
            Height = 1,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e)),
        };
        Grid.SetColumnSpan(_topBorder, 8);
        _rootGrid.Children.Add(_topBorder);

        _backButton = CreateTitleButton("\uE72B", "Back");
        Grid.SetColumn(_backButton, 0);
        _rootGrid.Children.Add(_backButton);

        _paneToggleButton = CreateTitleButton("\uE700", "Toggle navigation");
        Grid.SetColumn(_paneToggleButton, 1);
        _rootGrid.Children.Add(_paneToggleButton);

        _iconHost = new Viewbox
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
        };
        Grid.SetColumn(_iconHost, 2);
        _rootGrid.Children.Add(_iconHost);

        StackPanel titlePanel = new()
        {
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Orientation = Orientation.Horizontal,
        };
        _titleTextBlock = new TextBlock
        {
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
        };
        _subtitleTextBlock = new TextBlock
        {
            Margin = new Thickness(4, 0, 0, 0),
            FontSize = 12,
            Opacity = 0.72,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextWrapping = TextWrapping.NoWrap,
            Visibility = Visibility.Collapsed,
        };
        titlePanel.Children.Add(_titleTextBlock);
        titlePanel.Children.Add(_subtitleTextBlock);
        Grid.SetColumn(titlePanel, 3);
        _rootGrid.Children.Add(titlePanel);

        DragRegion = new Grid
        {
            Background = new SolidColorBrush(Colors.Transparent),
        };
        Grid.SetColumn(DragRegion, 2);
        Grid.SetColumnSpan(DragRegion, 3);
        _rootGrid.Children.Add(DragRegion);

        _contentPresenter = new ContentPresenter
        {
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
        };
        Grid.SetColumn(_contentPresenter, 5);
        _rootGrid.Children.Add(_contentPresenter);

        _footerPresenter = new ContentPresenter
        {
            Margin = new Thickness(4, 0, 8, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
        };
        Grid.SetColumn(_footerPresenter, 6);
        _rootGrid.Children.Add(_footerPresenter);

        CaptionButtons = new CaptionButtonsControl
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
        };
        Grid.SetColumn(CaptionButtons, 7);
        _rootGrid.Children.Add(CaptionButtons);

        Content = _rootGrid;

        Loaded += (_, _) => Configure();
        Unloaded += (_, _) => Reset();
        SizeChanged += (_, _) => Configure();
        ActualThemeChanged += (_, _) => UpdateTopBorder(_isActive);
        Update();
    }

    internal Grid DragRegion { get; }

    internal CaptionButtonsControl CaptionButtons { get; }

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

    public CoreIsland.Window? Window
    {
        get => (CoreIsland.Window?)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    public bool IsBackButtonEnabled
    {
        get => (bool)GetValue(IsBackButtonEnabledProperty);
        set => SetValue(IsBackButtonEnabledProperty, value);
    }

    public bool IsPaneToggleButtonVisible
    {
        get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
        set => SetValue(IsPaneToggleButtonVisibleProperty, value);
    }

    private bool _isActive = true;

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
        Window.SetCaptionButtons(CaptionButtons);
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
        Height = DisplayMode == TitleBarDisplayMode.Tall ? 48 : 40;
        _backButton.Visibility = IsBackButtonVisible ? Visibility.Visible : Visibility.Collapsed;
        _backButton.IsEnabled = IsBackButtonEnabled;
        _paneToggleButton.Visibility = IsPaneToggleButtonVisible ? Visibility.Visible : Visibility.Collapsed;
        _iconHost.Visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        _iconHost.Child = Icon is null ? null : new ContentPresenter { Content = Icon };
        _titleTextBlock.Text = Title;
        _subtitleTextBlock.Text = Subtitle;
        _subtitleTextBlock.Visibility = string.IsNullOrEmpty(Subtitle) ? Visibility.Collapsed : Visibility.Visible;
        _contentPresenter.Content = TitleBarContent;
        _contentPresenter.Visibility = TitleBarContent is null ? Visibility.Collapsed : Visibility.Visible;
        _footerPresenter.Content = Footer;
        _footerPresenter.Visibility = Footer is null ? Visibility.Collapsed : Visibility.Visible;

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
        _isActive = value;
        var color = value ? null : new SolidColorBrush(Color.FromArgb(0xff, 0x8e, 0x8e, 0x8e));
        _titleTextBlock.Foreground = color;
        _subtitleTextBlock.Foreground = color;
        CaptionButtons.IsWindowActive(value);
        UpdateTopBorder(value);
    }

    private void UpdateTopBorder(bool isActive)
    {
        _topBorder.Background = new SolidColorBrush(GetTopBorderColor(isActive));
        _topBorder.Visibility = Visibility.Visible;
    }

    private Color GetTopBorderColor(bool isActive)
    {
        if (isActive && ShouldShowAccentColorOnTitleBars())
            return GetDwmAccentColor();

        if (!isActive)
            return Color.FromArgb(0xff, 0x30, 0x30, 0x30);

        return ActualTheme == ElementTheme.Dark
            ? Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e)
            : Color.FromArgb(0x40, 0x00, 0x00, 0x00);
    }

    private void DetachWindow(CoreIsland.Window window)
    {
        window.Activated -= ConfiguredWindow_Activated;
        window.SizeChanged -= ConfiguredWindow_SizeChanged;
        window.SetCaptionButtons(null);
        window.SetTitleBar(null);
        window.ExtendsContentIntoTitleBar = false;
        _configuredWindow = null;
    }

    private Grid CreateRootGrid()
    {
        Grid grid = new()
        {
            Background = new SolidColorBrush(Colors.Transparent),
        };

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 4 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        return grid;
    }

    private static Button CreateTitleButton(string glyph, string automationName)
    {
        FontIcon icon = new()
        {
            Glyph = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 12,
        };

        Button button = new()
        {
            Content = icon,
            Width = 40,
            MinWidth = 40,
            Height = 32,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Colors.Transparent),
            IsTabStop = false,
        };
        Windows.UI.Xaml.Automation.AutomationProperties.SetName(button, automationName);
        return button;
    }

    private static bool ShouldShowAccentColorOnTitleBars()
    {
        return Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "ColorPrevalence", 0) is int value && value != 0;
    }

    private static Color GetDwmAccentColor()
    {
        var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", unchecked((int)0xffd77800)) is int color
            ? unchecked((uint)color)
            : 0xffd77800u;

        return Color.FromArgb(0xff, (byte)(value & 0xff), (byte)((value >> 8) & 0xff), (byte)((value >> 16) & 0xff));
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
        if (e.OldValue is CoreIsland.Window oldWindow)
            titleBar.DetachWindow(oldWindow);

        titleBar.Configure();
        titleBar.Update();
    }
}
