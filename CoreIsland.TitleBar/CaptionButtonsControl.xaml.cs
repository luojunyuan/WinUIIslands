using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using IslandWindow = CoreIsland.Window;

namespace CoreIsland.TitleBar;

public sealed partial class CaptionButtonsControl : UserControl, ICaptionButtons
{
    private CaptionButton? _pressedButton;
    private bool _allInNormal = true;
    private bool _isWindowMaximized;
    private bool _isWindowActive = true;

    public CaptionButtonsControl()
    {
        InitializeComponent();
    }

    public IslandWindow? Window { get; set; }

    public FrameworkElement Element => this;

    public FrameworkElement MinimizeButtonElement => MinimizeButton;

    public FrameworkElement MaximizeButtonElement => MaximizeButton;

    public FrameworkElement CloseButtonElement => CloseButton;

    public void HoverButton(CaptionButton button)
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

    public void PressButton(CaptionButton button)
    {
        _allInNormal = false;
        _pressedButton = button;

        GoToButtonState(MinimizeButton, button == CaptionButton.Minimize ? "Pressed" : "Normal");
        GoToButtonState(MaximizeButton, button == CaptionButton.Maximize ? "Pressed" : "Normal");
        GoToButtonState(CloseButton, button == CaptionButton.Close ? "Pressed" : "Normal");
    }

    public void ReleaseButton(CaptionButton button)
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
        VisualStateManager.GoToState(MinimizeButton, activeState, true);
        VisualStateManager.GoToState(MaximizeButton, activeState, true);
        VisualStateManager.GoToState(CloseButton, activeState, true);
    }

    public void IsWindowMaximized(bool value) => IsWindowMaximized(value, false);

    private void IsWindowMaximized(bool value, bool fromDispatcher)
    {
        if (_isWindowMaximized == value)
            return;

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

    public void IsWindowActive(bool value)
    {
        _isWindowActive = value;

        var activeState = value ? "Normal" : "NotActive";
        VisualStateManager.GoToState(MinimizeButton, activeState, false);
        VisualStateManager.GoToState(MaximizeButton, activeState, false);
        VisualStateManager.GoToState(CloseButton, activeState, false);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Minimize);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Maximize);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Close);

    private static void GoToButtonState(Button button, string state)
    {
        VisualStateManager.GoToState(button, state, false);
    }
}
