using Windows.UI.Xaml;

namespace App1;

public sealed partial class MainWindow : WinUIIslands.Window
{
    private bool _isCustomTitleBarEnabled = true;

    public MainWindow()
    {
        InitializeComponent();
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
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(WindowingTitleBar);
            return;
        }

        SetTitleBar(null);
        ExtendsContentIntoTitleBar = false;
    }
}
