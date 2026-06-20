using CoreIsland.Windowing;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using DrawingPoint = System.Drawing.Point;

namespace CoreIsland.TitleBar;

public sealed partial class CaptionButtonsControl : UserControl
{
    private const uint SC_MINIMIZE = 0xF020;
    private const uint SC_MAXIMIZE = 0xF030;
    private const uint SC_CLOSE = 0xF060;
    private const uint SC_RESTORE = 0xF120;

    private const double ButtonWidth = 46;
    private const double ButtonHeight = 32;

    private readonly Button _minimizeButton;
    private readonly Button _maximizeButton;
    private readonly Button _closeButton;
    private readonly FontIcon _maximizeIcon;

    private CaptionButton? _pressedButton;
    private CaptionButton? _hoveredButton;
    private bool _allInNormal = true;
    private bool _isWindowActive = true;
    private bool _isWindowMaximized;

    public CaptionButtonsControl()
    {
        Background = new SolidColorBrush(Colors.Transparent);

        _minimizeButton = CreateButton("\uE921", "Minimize", CaptionButton.Minimize);
        _maximizeButton = CreateButton("\uE922", "Maximize", CaptionButton.Maximize, out _maximizeIcon);
        _closeButton = CreateButton("\uE8BB", "Close", CaptionButton.Close);

        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
        };
        panel.Children.Add(_minimizeButton);
        panel.Children.Add(_maximizeButton);
        panel.Children.Add(_closeButton);
        Content = panel;

        ActualThemeChanged += (_, _) => LeaveButtons();
        IsWindowActive(true);
    }

    internal CoreIsland.Window? Window { get; set; }

    internal FrameworkElement MaximizeButtonElement => _maximizeButton;

    internal Size CaptionButtonSize => new(ButtonWidth, ButtonHeight);

    internal void HoverButton(CaptionButton button)
    {
        _hoveredButton = button;

        if (_pressedButton is CaptionButton pressedButton)
        {
            bool hoveringOnPressedButton = pressedButton == button;
            _allInNormal = !hoveringOnPressedButton;
            SetButtonState(_minimizeButton, CaptionButton.Minimize, hoveringOnPressedButton && button == CaptionButton.Minimize ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
            SetButtonState(_maximizeButton, CaptionButton.Maximize, hoveringOnPressedButton && button == CaptionButton.Maximize ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
            SetButtonState(_closeButton, CaptionButton.Close, hoveringOnPressedButton && button == CaptionButton.Close ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
            return;
        }

        _allInNormal = false;
        var activeState = _isWindowActive ? CaptionButtonVisualState.Normal : CaptionButtonVisualState.NotActive;
        SetButtonState(_minimizeButton, CaptionButton.Minimize, button == CaptionButton.Minimize ? CaptionButtonVisualState.PointerOver : activeState);
        SetButtonState(_maximizeButton, CaptionButton.Maximize, button == CaptionButton.Maximize ? CaptionButtonVisualState.PointerOver : activeState);
        SetButtonState(_closeButton, CaptionButton.Close, button == CaptionButton.Close ? CaptionButtonVisualState.PointerOver : activeState);
    }

    internal void PressButton(CaptionButton button)
    {
        _allInNormal = false;
        _pressedButton = button;
        _hoveredButton = button;

        SetButtonState(_minimizeButton, CaptionButton.Minimize, button == CaptionButton.Minimize ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
        SetButtonState(_maximizeButton, CaptionButton.Maximize, button == CaptionButton.Maximize ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
        SetButtonState(_closeButton, CaptionButton.Close, button == CaptionButton.Close ? CaptionButtonVisualState.Pressed : CaptionButtonVisualState.Normal);
    }

    internal void ReleaseButton(CaptionButton button)
    {
        bool clicked = _pressedButton == button;

        if (clicked)
            SendSystemCommand(button);

        _pressedButton = null;
        _allInNormal = clicked;
        _hoveredButton = clicked ? null : button;

        SetButtonState(_minimizeButton, CaptionButton.Minimize, !clicked && button == CaptionButton.Minimize ? CaptionButtonVisualState.PointerOver : CaptionButtonVisualState.Normal);
        SetButtonState(_maximizeButton, CaptionButton.Maximize, !clicked && button == CaptionButton.Maximize ? CaptionButtonVisualState.PointerOver : CaptionButtonVisualState.Normal);
        SetButtonState(_closeButton, CaptionButton.Close, !clicked && button == CaptionButton.Close ? CaptionButtonVisualState.PointerOver : CaptionButtonVisualState.Normal);
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
        _hoveredButton = null;
        var activeState = _isWindowActive ? CaptionButtonVisualState.Normal : CaptionButtonVisualState.NotActive;
        SetButtonState(_minimizeButton, CaptionButton.Minimize, activeState);
        SetButtonState(_maximizeButton, CaptionButton.Maximize, activeState);
        SetButtonState(_closeButton, CaptionButton.Close, activeState);
    }

    internal void IsWindowMaximized(bool value)
    {
        _isWindowMaximized = value;
        _maximizeIcon.Glyph = value ? "\uE923" : "\uE922";
        AutomationProperties.SetName(_maximizeButton, value ? "Restore" : "Maximize");
    }

    internal void IsWindowActive(bool value)
    {
        _isWindowActive = value;
        _allInNormal = false;
        LeaveButtons();
    }

    private Button CreateButton(string glyph, string automationName, CaptionButton button) =>
        CreateButton(glyph, automationName, button, out _);

    private Button CreateButton(string glyph, string automationName, CaptionButton button, out FontIcon icon)
    {
        icon = new FontIcon
        {
            Glyph = glyph,
            FontSize = 10,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
        };

        Button control = new()
        {
            Content = icon,
            Width = ButtonWidth,
            MinWidth = ButtonWidth,
            Height = ButtonHeight,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(Colors.Transparent),
            IsTabStop = false,
        };

        AutomationProperties.SetName(control, automationName);
        control.Click += (_, _) => SendSystemCommand(button);
        return control;
    }

    private void SendSystemCommand(CaptionButton button)
    {
        if (Window is null)
            return;

        uint command = button switch
        {
            CaptionButton.Minimize => SC_MINIMIZE,
            CaptionButton.Maximize => PInvoke.IsZoomed(new HWND(WindowNative.GetWindowHandle(Window))) ? SC_RESTORE : SC_MAXIMIZE,
            CaptionButton.Close => SC_CLOSE,
            _ => 0,
        };

        LPARAM lParam = default;
        if (button == CaptionButton.Maximize && PInvoke.GetCursorPos(out DrawingPoint point).Value != 0)
            lParam = new LPARAM(MakeLParam(point.X, point.Y));

        PInvoke.PostMessage(new HWND(WindowNative.GetWindowHandle(Window)), PInvoke.WM_SYSCOMMAND, new WPARAM((nuint)command), lParam);
    }

    private void SetButtonState(Button button, CaptionButton captionButton, CaptionButtonVisualState state)
    {
        button.Background = new SolidColorBrush(GetBackground(captionButton, state));
        if (button.Content is FontIcon icon)
            icon.Foreground = new SolidColorBrush(GetForeground(state));
    }

    private Color GetForeground(CaptionButtonVisualState state)
    {
        if (state == CaptionButtonVisualState.NotActive)
            return ActualTheme == ElementTheme.Dark ? Color.FromArgb(0x59, 0xff, 0xff, 0xff) : Color.FromArgb(0x61, 0x00, 0x00, 0x00);

        return ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
    }

    private Color GetBackground(CaptionButton button, CaptionButtonVisualState state)
    {
        if (button == CaptionButton.Close)
        {
            return state switch
            {
                CaptionButtonVisualState.PointerOver => Color.FromArgb(0xff, 0xc4, 0x2b, 0x1c),
                CaptionButtonVisualState.Pressed => Color.FromArgb(0xe6, 0xc4, 0x2b, 0x1c),
                _ => Colors.Transparent,
            };
        }

        byte alpha = state switch
        {
            CaptionButtonVisualState.PointerOver => 0x10,
            CaptionButtonVisualState.Pressed => 0x0a,
            _ => 0x00,
        };

        var foreground = ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
        return Color.FromArgb(alpha, foreground.R, foreground.G, foreground.B);
    }

    private static nint MakeLParam(int low, int high) => (short)low | ((nint)(short)high << 16);

    private enum CaptionButtonVisualState
    {
        Normal,
        PointerOver,
        Pressed,
        NotActive,
    }
}
