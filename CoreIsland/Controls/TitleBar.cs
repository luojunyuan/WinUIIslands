// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using MuxIconSource = Microsoft.UI.Xaml.Controls.IconSource;

namespace CoreIsland.Controls;

[ContentProperty(Name = nameof(Content))]
public partial class TitleBar : Control
{
    private const string BackButtonPartName = "PART_BackButton";
    private const string PaneToggleButtonPartName = "PART_PaneToggleButton";
    private const string IconViewboxPartName = "PART_Icon";
    private const string LeftHeaderPresenterPartName = "PART_LeftHeaderPresenter";
    private const string ContentPresenterGridPartName = "PART_ContentPresenterGrid";
    private const string ContentPresenterPartName = "PART_ContentPresenter";
    private const string RightHeaderPresenterPartName = "PART_RightHeaderPresenter";

    private readonly ResourceDictionary _resources;
    private readonly long _flowDirectionChangedToken;

    private ColumnDefinition? _leftPaddingColumn;
    private ColumnDefinition? _rightPaddingColumn;
    private Button? _backButton;
    private Button? _paneToggleButton;
    private FrameworkElement? _iconViewbox;
    private Grid? _contentAreaGrid;
    private FrameworkElement? _leftHeaderArea;
    private FrameworkElement? _contentArea;
    private FrameworkElement? _rightHeaderArea;
    private FrameworkElement? _contentLayoutElement;
    private FrameworkElement? _iconLayoutElement;

    private double _compactModeThresholdWidth;
    private bool _isCompact;
    private bool _isWindowActive = true;

    public TitleBar()
    {
        SetValue(TemplateSettingsProperty, new TitleBarTemplateSettings());

        _resources = LoadTitleBarResources();
        Resources.MergedDictionaries.Add(_resources);
        Style = (Style)_resources["DefaultTitleBarStyle"];

        SizeChanged += OnSizeChanged;
        _flowDirectionChangedToken = RegisterPropertyChangedCallback(FlowDirectionProperty, OnFlowDirectionChanged);
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(TitleBar),
        new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(TitleBar),
        new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
        nameof(IconSource),
        typeof(MuxIconSource),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty LeftHeaderProperty = DependencyProperty.Register(
        nameof(LeftHeader),
        typeof(UIElement),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content),
        typeof(UIElement),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty RightHeaderProperty = DependencyProperty.Register(
        nameof(RightHeader),
        typeof(UIElement),
        typeof(TitleBar),
        new PropertyMetadata(null, OnPropertyChanged));

    public static readonly DependencyProperty IsBackButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsBackButtonVisible),
        typeof(bool),
        typeof(TitleBar),
        new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty IsBackButtonEnabledProperty = DependencyProperty.Register(
        nameof(IsBackButtonEnabled),
        typeof(bool),
        typeof(TitleBar),
        new PropertyMetadata(true, OnPropertyChanged));

    public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsPaneToggleButtonVisible),
        typeof(bool),
        typeof(TitleBar),
        new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty AutoRefreshDragRegionsProperty = DependencyProperty.Register(
        nameof(AutoRefreshDragRegions),
        typeof(bool),
        typeof(TitleBar),
        new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty TemplateSettingsProperty = DependencyProperty.Register(
        nameof(TemplateSettings),
        typeof(TitleBarTemplateSettings),
        typeof(TitleBar),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsDragRegionProperty = DependencyProperty.RegisterAttached(
        "IsDragRegion",
        typeof(bool?),
        typeof(TitleBar),
        new PropertyMetadata(null, OnIsDragRegionPropertyChanged));

    public string Title
    {
        get => GetValue(TitleProperty) as string ?? string.Empty;
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => GetValue(SubtitleProperty) as string ?? string.Empty;
        set => SetValue(SubtitleProperty, value);
    }

    public MuxIconSource? IconSource
    {
        get => (MuxIconSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public UIElement? LeftHeader
    {
        get => (UIElement?)GetValue(LeftHeaderProperty);
        set => SetValue(LeftHeaderProperty, value);
    }

    public UIElement? Content
    {
        get => (UIElement?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public UIElement? RightHeader
    {
        get => (UIElement?)GetValue(RightHeaderProperty);
        set => SetValue(RightHeaderProperty, value);
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

    public bool AutoRefreshDragRegions
    {
        get => (bool)GetValue(AutoRefreshDragRegionsProperty);
        set => SetValue(AutoRefreshDragRegionsProperty, value);
    }

    public TitleBarTemplateSettings TemplateSettings =>
        (TitleBarTemplateSettings)GetValue(TemplateSettingsProperty);

    public event TypedEventHandler<TitleBar, object?>? BackRequested;

    public event TypedEventHandler<TitleBar, object?>? PaneToggleRequested;

    public static bool? GetIsDragRegion(UIElement element) =>
        (bool?)element.GetValue(IsDragRegionProperty);

    public static void SetIsDragRegion(UIElement element, bool? value) =>
        element.SetValue(IsDragRegionProperty, value);

    public void RecomputeDragRegions()
    {
        UpdateLayout();
        UpdateInteractableElementsList();
        UpdateDragRegion(force: true);
    }

    protected override void OnApplyTemplate()
    {
        DetachTemplateHandlers();
        base.OnApplyTemplate();

        // NativeAOT can project non-FrameworkElement template parts returned by GetTemplateChild
        // as DependencyObject, so direct ColumnDefinition casts that work under JIT fail. Access
        // the columns through the Grid's typed collection to preserve their projected type.
        var layoutRoot = GetTemplateChild("PART_LayoutRoot") as Grid;
        var columnDefinitions = layoutRoot?.ColumnDefinitions;
        _leftPaddingColumn = columnDefinitions is { Count: > 1 } ? columnDefinitions[0] : null;
        _rightPaddingColumn = columnDefinitions is { Count: > 1 } ? columnDefinitions[columnDefinitions.Count - 1] : null;
        _backButton = GetTemplateChild(BackButtonPartName) as Button;
        _paneToggleButton = GetTemplateChild(PaneToggleButtonPartName) as Button;
        _iconViewbox = GetTemplateChild(IconViewboxPartName) as FrameworkElement;
        _leftHeaderArea = GetTemplateChild(LeftHeaderPresenterPartName) as FrameworkElement;
        _contentAreaGrid = GetTemplateChild(ContentPresenterGridPartName) as Grid;
        _contentArea = GetTemplateChild(ContentPresenterPartName) as FrameworkElement;
        _rightHeaderArea = GetTemplateChild(RightHeaderPresenterPartName) as FrameworkElement;

        LoadBackButton();
        LoadPaneToggleButton();
        UpdateHeight();
        UpdatePadding();
        UpdateIcon();
        UpdateBackButton();
        UpdatePaneToggleButton();
        UpdateTitle();
        UpdateSubtitle();
        UpdateLeftHeader();
        UpdateContent();
        UpdateRightHeader();
        UpdateLeftHeaderSpacing();
        UpdateActivationStates();
        UpdateInteractableElementsList();
        UpdateDragRegion(force: true);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TitleBarAutomationPeer(this);

    private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var titleBar = (TitleBar)sender;

        if (args.Property == AutoRefreshDragRegionsProperty)
        {
            titleBar.UpdateAutoRefreshDragRegions();
        }
        else if (args.Property == IsBackButtonVisibleProperty)
        {
            titleBar.UpdateBackButton();
        }
        else if (args.Property == IsBackButtonEnabledProperty)
        {
            titleBar.UpdateInteractableElementsList();
        }
        else if (args.Property == IsPaneToggleButtonVisibleProperty)
        {
            titleBar.UpdatePaneToggleButton();
        }
        else if (args.Property == IconSourceProperty)
        {
            titleBar.UpdateIcon();
        }
        else if (args.Property == TitleProperty)
        {
            titleBar.HandleTitleChange(args.OldValue as string ?? string.Empty, titleBar.Title);
        }
        else if (args.Property == SubtitleProperty)
        {
            titleBar.UpdateSubtitle();
        }
        else if (args.Property == LeftHeaderProperty)
        {
            titleBar.UpdateLeftHeader();
        }
        else if (args.Property == ContentProperty)
        {
            titleBar.UpdateContent();
        }
        else if (args.Property == RightHeaderProperty)
        {
            titleBar.UpdateRightHeader();
        }

        titleBar.UpdateInteractableElementsList();
        titleBar.UpdateDragRegion();
    }

    private static void OnIsDragRegionPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        DependencyObject? current = sender;
        while (current is not null)
        {
            if (current is TitleBar titleBar)
            {
                titleBar.UpdateInteractableElementsList();
                titleBar.UpdateDragRegion();
                return;
            }

            current = VisualTreeHelper.GetParent(current);
        }
    }

    private void HandleTitleChange(string oldTitle, string newTitle)
    {
        if (!string.IsNullOrEmpty(oldTitle) && string.IsNullOrEmpty(newTitle))
        {
            ResetWindowTitle(oldTitle);
            GoToState("TitleTextCollapsed");
            return;
        }

        UpdateTitle();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs args)
    {
        if (Content is not null && _contentArea is not null && _contentAreaGrid is not null)
        {
            if (_compactModeThresholdWidth == 0 &&
                _contentArea.DesiredSize.Width >= _contentAreaGrid.ActualWidth)
            {
                _compactModeThresholdWidth = args.NewSize.Width;
                _isCompact = true;
                GoToState("Compact");
            }
            else if (_isCompact && args.NewSize.Width >= _compactModeThresholdWidth)
            {
                _compactModeThresholdWidth = 0;
                _isCompact = false;
                GoToState("Expanded");
                UpdateTitle();
                UpdateSubtitle();
            }
        }

        UpdateDragRegion();
    }

    private void OnFlowDirectionChanged(DependencyObject sender, DependencyProperty property) => UpdatePadding();

    private void OnBackButtonClick(object sender, RoutedEventArgs args) => BackRequested?.Invoke(this, null);

    private void OnPaneToggleButtonClick(object sender, RoutedEventArgs args) => PaneToggleRequested?.Invoke(this, null);

    private void OnIconLayoutUpdated(object? sender, object args)
    {
        UpdateInteractableElementsList();
        UpdateDragRegion(force: true);
    }

    private void OnContentLayoutUpdated(object? sender, object args)
    {
        UpdateInteractableElementsList();
        UpdateDragRegion();

        if (!AutoRefreshDragRegions)
            DetachContentLayoutHandler();
    }

    private void UpdateIcon()
    {
        DetachIconLayoutHandler();

        if (IconSource is not null)
        {
            TemplateSettings.IconElement = IconSource.CreateIconElement();
            GoToState(_isWindowActive ? "IconVisible" : "IconDeactivated");

            if (_iconViewbox is not null)
            {
                _iconLayoutElement = _iconViewbox;
                _iconLayoutElement.LayoutUpdated += OnIconLayoutUpdated;
            }
        }
        else
        {
            TemplateSettings.IconElement = null;
            GoToState("IconCollapsed");
        }

        UpdateDragRegion(force: true);
    }

    private void UpdateBackButton()
    {
        GoToState(IsBackButtonVisible
            ? !_isWindowActive && IsBackButtonEnabled ? "BackButtonDeactivated" : "BackButtonVisible"
            : "BackButtonCollapsed");

        UpdateInteractableElementsList();
        UpdateLeftHeaderSpacing();
    }

    private void UpdatePaneToggleButton()
    {
        GoToState(IsPaneToggleButtonVisible
            ? _isWindowActive ? "PaneToggleButtonVisible" : "PaneToggleButtonDeactivated"
            : "PaneToggleButtonCollapsed");

        UpdateInteractableElementsList();
        UpdateLeftHeaderSpacing();
    }

    private void UpdateHeight() => GoToState(
        Content is null && LeftHeader is null && RightHeader is null ? "CompactHeight" : "ExpandedHeight");

    private void UpdatePadding()
    {
        if (_leftPaddingColumn is null || _rightPaddingColumn is null)
            return;

        var (leftInset, rightInset) = _window?.GetTitleBarInsets() ?? (2d, 0d);
        _leftPaddingColumn.Width = new GridLength(FlowDirection == FlowDirection.LeftToRight ? leftInset : rightInset);
        _rightPaddingColumn.Width = new GridLength(FlowDirection == FlowDirection.LeftToRight ? rightInset : leftInset);
    }

    private void UpdateTitle()
    {
        UpdateWindowTitle();
        GoToState(string.IsNullOrEmpty(Title)
            ? "TitleTextCollapsed"
            : _isWindowActive ? "TitleTextVisible" : "TitleTextDeactivated");
    }

    private void UpdateSubtitle() => GoToState(string.IsNullOrEmpty(Subtitle)
        ? "SubtitleTextCollapsed"
        : _isWindowActive ? "SubtitleTextVisible" : "SubtitleTextDeactivated");

    private void UpdateLeftHeader()
    {
        GoToState(LeftHeader is null
            ? "LeftHeaderCollapsed"
            : _isWindowActive ? "LeftHeaderVisible" : "LeftHeaderDeactivated");
        UpdateHeight();
        UpdateInteractableElementsList();
    }

    private void UpdateContent()
    {
        DetachContentLayoutHandler();

        if (Content is null)
        {
            GoToState("ContentCollapsed");
        }
        else
        {
            if (Content is FrameworkElement content)
            {
                _contentLayoutElement = content;
                _contentLayoutElement.LayoutUpdated += OnContentLayoutUpdated;
            }

            GoToState(_isWindowActive ? "ContentVisible" : "ContentDeactivated");
        }

        UpdateHeight();
        UpdateInteractableElementsList();
    }

    private void UpdateRightHeader()
    {
        GoToState(RightHeader is null
            ? "RightHeaderCollapsed"
            : _isWindowActive ? "RightHeaderVisible" : "RightHeaderDeactivated");
        UpdateHeight();
        UpdateInteractableElementsList();
    }

    private void UpdateLeftHeaderSpacing() => GoToState(
        IsBackButtonVisible == IsPaneToggleButtonVisible ? "DefaultSpacing" : "NegativeInsetSpacing");

    private void UpdateAutoRefreshDragRegions()
    {
        if (AutoRefreshDragRegions)
        {
            if (_contentLayoutElement is null && Content is FrameworkElement content)
            {
                _contentLayoutElement = content;
                _contentLayoutElement.LayoutUpdated += OnContentLayoutUpdated;
            }

            UpdateInteractableElementsList();
        }
        else
        {
            DetachContentLayoutHandler();
        }
    }

    private void UpdateActivationStates()
    {
        if (IsBackButtonVisible)
            GoToState(!_isWindowActive && IsBackButtonEnabled ? "BackButtonDeactivated" : "BackButtonVisible");
        if (IsPaneToggleButtonVisible)
            GoToState(_isWindowActive ? "PaneToggleButtonVisible" : "PaneToggleButtonDeactivated");
        if (IconSource is not null)
            GoToState(_isWindowActive ? "IconVisible" : "IconDeactivated");
        if (!string.IsNullOrEmpty(Title) && !_isCompact)
            GoToState(_isWindowActive ? "TitleTextVisible" : "TitleTextDeactivated");
        if (!string.IsNullOrEmpty(Subtitle) && !_isCompact)
            GoToState(_isWindowActive ? "SubtitleTextVisible" : "SubtitleTextDeactivated");
        if (LeftHeader is not null)
            GoToState(_isWindowActive ? "LeftHeaderVisible" : "LeftHeaderDeactivated");
        if (Content is not null)
            GoToState(_isWindowActive ? "ContentVisible" : "ContentDeactivated");
        if (RightHeader is not null)
            GoToState(_isWindowActive ? "RightHeaderVisible" : "RightHeaderDeactivated");
    }

    private void LoadBackButton()
    {
        if (_backButton is null)
            return;

        _backButton.Click += OnBackButtonClick;
        SetAccessibleNameAndTooltip(
            _backButton,
            "NavigationBackButtonName",
            "NavigationBackButtonToolTip",
            "Back");
    }

    private void LoadPaneToggleButton()
    {
        if (_paneToggleButton is null)
            return;

        _paneToggleButton.Click += OnPaneToggleButtonClick;
        SetAccessibleNameAndTooltip(
            _paneToggleButton,
            "NavigationButtonToggleName",
            "NavigationButtonToggleName",
            "Toggle navigation");
    }

    private void DetachTemplateHandlers()
    {
        if (_backButton is not null)
            _backButton.Click -= OnBackButtonClick;
        if (_paneToggleButton is not null)
            _paneToggleButton.Click -= OnPaneToggleButtonClick;

        DetachContentLayoutHandler();
        DetachIconLayoutHandler();
    }

    private void DetachContentLayoutHandler()
    {
        if (_contentLayoutElement is null)
            return;

        _contentLayoutElement.LayoutUpdated -= OnContentLayoutUpdated;
        _contentLayoutElement = null;
    }

    private void DetachIconLayoutHandler()
    {
        if (_iconLayoutElement is null)
            return;

        _iconLayoutElement.LayoutUpdated -= OnIconLayoutUpdated;
        _iconLayoutElement = null;
    }

    private void GoToState(string stateName) => VisualStateManager.GoToState(this, stateName, false);

    private static void SetAccessibleNameAndTooltip(
        Button button,
        string nameResource,
        string tooltipResource,
        string fallback)
    {
        var name = TryGetMuxString(nameResource) ?? fallback;
        var tooltip = TryGetMuxString(tooltipResource) ?? name;

        if (string.IsNullOrEmpty(AutomationProperties.GetName(button)))
            AutomationProperties.SetName(button, name);

        ToolTipService.SetToolTip(button, new ToolTip { Content = tooltip });
    }

    private static string? TryGetMuxString(string resourceName)
    {
        try
        {
            var value = ResourceLoader.GetForViewIndependentUse("Microsoft.UI.Xaml/Resources").GetString(resourceName);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private static ResourceDictionary LoadTitleBarResources()
    {
        var themeResources = ReadEmbeddedText("CoreIsland.Controls.TitleBar_themeresources.xaml");
        var style = ReadEmbeddedText("CoreIsland.Controls.TitleBar.xaml");
        var xaml = $$"""
<ResourceDictionary
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:controls="using:Microsoft.UI.Xaml.Controls"
    xmlns:animatedvisuals="using:Microsoft.UI.Xaml.Controls.AnimatedVisuals"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
{{GetResourceDictionaryContents(themeResources)}}
{{style}}
</ResourceDictionary>
""";

        return (ResourceDictionary)XamlReader.Load(xaml);
    }

    private static string GetResourceDictionaryContents(string xaml)
    {
        var rootStart = xaml.IndexOf("<ResourceDictionary", StringComparison.Ordinal);
        if (rootStart < 0)
            throw new InvalidOperationException("The embedded TitleBar resource dictionary is invalid.");

        var contentStart = xaml.IndexOf('>', rootStart) + 1;
        var contentEnd = xaml.LastIndexOf("</ResourceDictionary>", StringComparison.Ordinal);

        if (contentStart <= 0 || contentEnd < contentStart)
            throw new InvalidOperationException("The embedded TitleBar resource dictionary is invalid.");

        return xaml[contentStart..contentEnd];
    }

    private static string ReadEmbeddedText(string resourceName)
    {
        using var stream = typeof(TitleBar).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded TitleBar resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
