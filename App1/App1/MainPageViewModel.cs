using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI.Xaml;

namespace App1;

public sealed partial class MainPageViewModel : ObservableObject
{
    private int _theme;
    private int _backdrop = MainPage.IsMicaAvailable ? 2 : 0;

    public MainPageViewModel(WinUIIslands.Window? hostWindow = null)
    {
        HostWindow = hostWindow;
    }

    public WinUIIslands.Window? HostWindow { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomTitleBarVisibility))]
    public partial bool IsCustomTitleBarEnabled { get; set; } = true;

    public Visibility CustomTitleBarVisibility => IsCustomTitleBarEnabled ? Visibility.Visible : Visibility.Collapsed;

    public int Theme
    {
        get => _theme;
        set
        {
            if (value < 0)
                return;

            SetProperty(ref _theme, value);
        }
    }

    public int Backdrop
    {
        get => _backdrop;
        set
        {
            if (value < 0)
                return;

            if (!MainPage.IsMicaAvailable && value >= 2)
                value = 0;

            SetProperty(ref _backdrop, value);
        }
    }

    partial void OnIsCustomTitleBarEnabledChanged(bool value)
    {
        if (HostWindow is MainWindow mainWindow)
            mainWindow.IsCustomTitleBarEnabled = value;
    }
}
