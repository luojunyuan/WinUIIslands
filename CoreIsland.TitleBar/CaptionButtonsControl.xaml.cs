using CoreIsland.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;

namespace CoreIsland.TitleBar;

public sealed partial class CaptionButtonsControl : UserControl
{
    private const int SC_MINIMIZE = 0xF020;
    private const int SC_MAXIMIZE = 0xF030;
    private const int SC_CLOSE = 0xF060;
    private const int SC_RESTORE = 0xF120;

    public CaptionButtonsControl()
    {
        InitializeComponent();
    }

    internal Window? Window { get; set; }

    internal void IsWindowMaximized(bool value)
    {
        VisualStateManager.GoToState(MaximizeButton, value ? "WindowStateMaximized" : "WindowStateNormal", false);
        AutomationProperties.SetName(MaximizeButton, value ? "Restore" : "Maximize");
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => SendSystemCommand(SC_MINIMIZE);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (Window is null)
            return;

        var hwnd = new HWND(WindowNative.GetWindowHandle(Window));
        SendSystemCommand(PInvoke.IsZoomed(hwnd) ? SC_RESTORE : SC_MAXIMIZE);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => SendSystemCommand(SC_CLOSE);

    private void SendSystemCommand(int command)
    {
        if (Window is null)
            return;

        PInvoke.PostMessage(new HWND(WindowNative.GetWindowHandle(Window)), PInvoke.WM_SYSCOMMAND, new WPARAM((nuint)command), default);
    }
}
