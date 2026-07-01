using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using IslandWindow = CoreIsland.Window;

namespace CoreIsland.TitleBar;

internal sealed partial class CaptionButtonsControl : UserControl, ICaptionButtons
{
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    private readonly Button _minimizeButton;
    private readonly Button _maximizeButton;
    private readonly Button _closeButton;
    private readonly ResourceDictionary _captionButtonResources;

    private CaptionButton? _pressedButton;
    private bool _allInNormal = true;
    private bool _isWindowMaximized;
    private bool _isWindowActive = true;

    public CaptionButtonsControl()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Top;
        Background = TransparentBrush;
        Visibility = Visibility.Collapsed;

        _captionButtonResources = (ResourceDictionary)XamlReader.Load(CaptionButtonResourcesXaml);
        Resources.MergedDictionaries.Add(_captionButtonResources);

        _minimizeButton = CreateButton("Minimize", "CaptionButtonStyle", "\uE921");
        _maximizeButton = CreateButton("Maximize", "CaptionButtonStyle", "\uE922");
        _closeButton = CreateButton("Close", "CloseCaptionButtonStyle", "\uE8BB");

        _minimizeButton.Click += MinimizeButton_Click;
        _maximizeButton.Click += MaximizeButton_Click;
        _closeButton.Click += CloseButton_Click;

        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                _minimizeButton,
                _maximizeButton,
                _closeButton,
            },
        };

        ApplyNormalState();
    }

    public IslandWindow? Window { get; set; }

    public FrameworkElement Element => this;

    public FrameworkElement MinimizeButtonElement => _minimizeButton;

    public FrameworkElement MaximizeButtonElement => _maximizeButton;

    public FrameworkElement CloseButtonElement => _closeButton;

    public void HoverButton(CaptionButton button)
    {
        if (_pressedButton is CaptionButton pressedButton)
        {
            bool hoveringOnPressedButton = pressedButton == button;
            _allInNormal = !hoveringOnPressedButton;
            GoToButtonState(_minimizeButton, hoveringOnPressedButton && button == CaptionButton.Minimize ? "Pressed" : "Normal");
            GoToButtonState(_maximizeButton, hoveringOnPressedButton && button == CaptionButton.Maximize ? "Pressed" : "Normal");
            GoToButtonState(_closeButton, hoveringOnPressedButton && button == CaptionButton.Close ? "Pressed" : "Normal");
            return;
        }

        _allInNormal = false;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, button == CaptionButton.Minimize ? "PointerOver" : activeState);
        GoToButtonState(_maximizeButton, button == CaptionButton.Maximize ? "PointerOver" : activeState);
        GoToButtonState(_closeButton, button == CaptionButton.Close ? "PointerOver" : activeState);
    }

    public void PressButton(CaptionButton button)
    {
        _allInNormal = false;
        _pressedButton = button;

        GoToButtonState(_minimizeButton, button == CaptionButton.Minimize ? "Pressed" : "Normal");
        GoToButtonState(_maximizeButton, button == CaptionButton.Maximize ? "Pressed" : "Normal");
        GoToButtonState(_closeButton, button == CaptionButton.Close ? "Pressed" : "Normal");
    }

    public void ReleaseButton(CaptionButton button)
    {
        bool clicked = _pressedButton == button;

        if (clicked)
            Window?.PostCaptionButtonCommand(button);

        _pressedButton = null;
        _allInNormal = clicked;

        GoToButtonState(_minimizeButton, !clicked && button == CaptionButton.Minimize ? "PointerOver" : "Normal");
        GoToButtonState(_maximizeButton, !clicked && button == CaptionButton.Maximize ? "PointerOver" : "Normal");
        GoToButtonState(_closeButton, !clicked && button == CaptionButton.Close ? "PointerOver" : "Normal");
    }

    public void ReleaseButtons()
    {
        if (_pressedButton is null)
            return;

        _pressedButton = null;
        LeaveButtons();
    }

    public void LeaveButtons()
    {
        if (_allInNormal)
            return;

        _allInNormal = true;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    public void IsWindowMaximized(bool value) => IsWindowMaximized(value, false);

    private void IsWindowMaximized(bool value, bool fromDispatcher)
    {
        if (_isWindowMaximized == value)
            return;

        if (!fromDispatcher && _maximizeButton.Dispatcher.HasThreadAccess is false)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
            return;
        }

        if (VisualStateManager.GoToState(_maximizeButton, value ? "WindowStateMaximized" : "WindowStateNormal", false))
        {
            _isWindowMaximized = value;
            AutomationProperties.SetName(_maximizeButton, value ? "Restore" : "Maximize");
        }
        else if (!fromDispatcher)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
        }
    }

    public void IsWindowActive(bool value)
    {
        _isWindowActive = value;

        var activeState = value ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Minimize);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Maximize);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Close);

    private Button CreateButton(string automationName, string styleKey, string glyph)
    {
        var button = new Button
        {
            Style = (Style)_captionButtonResources[styleKey],
            IsTabStop = false,
        };

        button.Resources["CaptionButtonGlyph"] = glyph;
        AutomationProperties.SetName(button, automationName);
        return button;
    }

    private void ApplyNormalState()
    {
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    private static void GoToButtonState(Button button, string state) => VisualStateManager.GoToState(button, state, false);
}
