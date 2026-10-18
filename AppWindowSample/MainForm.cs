using System;
using System.Drawing;
using System.Windows.Forms;
using WinUIIslands.Windowing;

namespace AppWindowSample;

internal sealed class MainForm : Form
{
    private readonly Label _status;

    public MainForm()
    {
        Text = "AppWindow.SetIcon sample";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 180);
        MinimumSize = new Size(420, 180);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(24),
            WrapContents = false,
        };

        var heading = new Label
        {
            AutoSize = true,
            Text = "AppWindow.SetIcon",
            Margin = new Padding(0, 0, 0, 16),
        };

        var setIconButton = new Button
        {
            AutoSize = true,
            Text = "Apply Assets/AppIcon.ico",
            Margin = new Padding(0, 0, 0, 12),
        };
        setIconButton.Click += SetIconButton_Click;

        _status = new Label
        {
            AutoSize = true,
            Text = "Not applied",
        };

        layout.Controls.Add(heading);
        layout.Controls.Add(setIconButton);
        layout.Controls.Add(_status);
        Controls.Add(layout);
    }

    private void SetIconButton_Click(object? sender, EventArgs e)
    {
        try
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(Handle);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetIcon("Assets/AppIcon.ico");
            _status.Text = "Applied to the title bar and taskbar.";
        }
        catch (Exception exception)
        {
            _status.Text = exception.Message;
        }
    }
}
