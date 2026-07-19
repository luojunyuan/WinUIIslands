using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using WinRT;
using IslandWindow = CoreIsland.Window;

namespace CoreIsland.TitleBar;

internal sealed partial class CaptionButtonsControl : Control
{
    private const string XamlResourceName = "CoreIsland.TitleBar.CaptionButtonsControl.xaml";

    private readonly ResourceDictionary _resources;
    private Button? _minimizeButton;
    private Button? _maximizeButton;
    private Button? _closeButton;

    private CaptionButton? _pressedButton;
    private bool _allInNormal = true;
    private bool _isWindowMaximized;
    private bool _isWindowActive = true;

    public CaptionButtonsControl()
    {
        _resources = LoadResources();
        Resources.MergedDictionaries.Add(_resources);
        Style = (Style)_resources["CaptionButtonsControlStyle"];
    }

    protected override void OnApplyTemplate()
    {
        _minimizeButton?.Click -= MinimizeButton_Click;
        _maximizeButton?.Click -= MaximizeButton_Click;
        _closeButton?.Click -= CloseButton_Click;

        base.OnApplyTemplate();

        _minimizeButton = GetButton("MinimizeButton");
        _maximizeButton = GetButton("MaximizeButton");
        _closeButton = GetButton("CloseButton");

        _minimizeButton.Click += MinimizeButton_Click;
        _maximizeButton.Click += MaximizeButton_Click;
        _closeButton.Click += CloseButton_Click;

        ApplyNormalState();
    }

    internal IslandWindow? Window { get; set; }

    internal void HoverButton(CaptionButton button)
    {
        if (_pressedButton is CaptionButton pressedButton)
        {
            bool hoveringOnPressedButton = pressedButton == button;
            _allInNormal = !hoveringOnPressedButton;
            GoToButtonState(MinimizeButton, hoveringOnPressedButton && button == CaptionButton.Minimize ? "Pressed" : "Normal");
            GoToButtonState(MaximizeButton, hoveringOnPressedButton && button == CaptionButton.Maximize ? "Pressed" : "Normal");
            GoToButtonState(CloseButton, hoveringOnPressedButton && button == CaptionButton.Close ? "Pressed" : "Normal");
            return;
        }

        _allInNormal = false;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(MinimizeButton, button == CaptionButton.Minimize ? "PointerOver" : activeState);
        GoToButtonState(MaximizeButton, button == CaptionButton.Maximize ? "PointerOver" : activeState);
        GoToButtonState(CloseButton, button == CaptionButton.Close ? "PointerOver" : activeState);
    }

    internal void PressButton(CaptionButton button)
    {
        _allInNormal = false;
        _pressedButton = button;

        GoToButtonState(MinimizeButton, button == CaptionButton.Minimize ? "Pressed" : "Normal");
        GoToButtonState(MaximizeButton, button == CaptionButton.Maximize ? "Pressed" : "Normal");
        GoToButtonState(CloseButton, button == CaptionButton.Close ? "Pressed" : "Normal");
    }

    internal void ReleaseButton(CaptionButton button)
    {
        bool clicked = _pressedButton == button;

        if (clicked)
            Window?.PostCaptionButtonCommand(button);

        _pressedButton = null;
        _allInNormal = clicked;

        GoToButtonState(MinimizeButton, !clicked && button == CaptionButton.Minimize ? "PointerOver" : "Normal");
        GoToButtonState(MaximizeButton, !clicked && button == CaptionButton.Maximize ? "PointerOver" : "Normal");
        GoToButtonState(CloseButton, !clicked && button == CaptionButton.Close ? "PointerOver" : "Normal");
    }

    internal void ReleaseButtons()
    {
        if (_pressedButton is null)
            return;

        _pressedButton = null;
        LeaveButtons();
    }

    internal void LeaveButtons()
    {
        if (_allInNormal)
            return;

        _allInNormal = true;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(MinimizeButton, activeState);
        GoToButtonState(MaximizeButton, activeState);
        GoToButtonState(CloseButton, activeState);
    }

    internal void IsWindowMaximized(bool value) => IsWindowMaximized(value, false);

    private void IsWindowMaximized(bool value, bool fromDispatcher)
    {
        if (_isWindowMaximized == value)
            return;

        if (!fromDispatcher && MaximizeButton.Dispatcher.HasThreadAccess is false)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
            return;
        }

        if (VisualStateManager.GoToState(MaximizeButton, value ? "WindowStateMaximized" : "WindowStateNormal", false))
        {
            _isWindowMaximized = value;
            AutomationProperties.SetName(MaximizeButton, value ? "Restore" : "Maximize");
        }
        else if (!fromDispatcher)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
        }
    }

    internal void IsWindowActive(bool value)
    {
        _isWindowActive = value;

        var activeState = value ? "Normal" : "NotActive";
        GoToButtonState(MinimizeButton, activeState);
        GoToButtonState(MaximizeButton, activeState);
        GoToButtonState(CloseButton, activeState);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Minimize);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Maximize);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Close);

    internal Button MinimizeButton
    {
        get
        {
            EnsureTemplateParts();
            return _minimizeButton!;
        }
    }

    internal Button MaximizeButton
    {
        get
        {
            EnsureTemplateParts();
            return _maximizeButton!;
        }
    }

    internal Button CloseButton
    {
        get
        {
            EnsureTemplateParts();
            return _closeButton!;
        }
    }

    private void EnsureTemplateParts()
    {
        if (_minimizeButton is not null && _maximizeButton is not null && _closeButton is not null)
            return;

        ApplyTemplate();

        if (_minimizeButton is null || _maximizeButton is null || _closeButton is null)
            throw new InvalidOperationException("The caption buttons template has not been applied.");
    }

    private static ResourceDictionary LoadResources()
    {
        using var stream = typeof(CaptionButtonsControl).Assembly.GetManifestResourceStream(XamlResourceName)
            ?? throw new InvalidOperationException($"Embedded caption buttons resource '{XamlResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return (ResourceDictionary)XamlReader.Load(reader.ReadToEnd());
    }

    private Button GetButton(string name)
    {
        var part = GetTemplateChild(name)
            ?? throw new InvalidOperationException($"Caption button '{name}' was not found in the embedded XAML.");

        // NativeAOT can keep dynamically loaded template parts projected as DependencyObject.
        // Query the WinRT object for Button when the normal managed cast cannot recover the class.
        return part as Button ?? part.As<Button>();
    }

    private void ApplyNormalState()
    {
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(MinimizeButton, activeState);
        GoToButtonState(MaximizeButton, activeState);
        GoToButtonState(CloseButton, activeState);
    }

    private static void GoToButtonState(Button button, string state) => VisualStateManager.GoToState(button, state, false);
}
