using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using WinUIIslands;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace App1;

public sealed partial class MainPage : Page
{
    private readonly UISettings _uiSettings = new();
    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        Initialize();
    }

    public MainPage(WinUIIslands.Window hostWindow)
    {
        HostWindow = hostWindow;
        ViewModel.HostWindow = hostWindow;
        InitializeComponent();
        Initialize();
    }

    public WinUIIslands.Window? HostWindow { get; private set; }

    public static bool IsMicaAvailable => QueryWindowsBuildNumber() >= 22621;

    private void Initialize()
    {
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        _uiSettings.ColorValuesChanged += UiSettings_ColorValuesChanged;
        StringResTextBlock.Text = LoadStringResource("Hello", "\"Hello\"");
        ApplyTheme();
        ApplyBackdrop();
    }

    private void RootPage_Loaded(object sender, RoutedEventArgs e)
    {
        HostWindow ??= WinUIIslands.Application.Current.MainWindow;
        ViewModel.HostWindow = HostWindow;
        if (HostWindow is MainWindow mainWindow)
            ViewModel.IsCustomTitleBarEnabled = mainWindow.IsCustomTitleBarEnabled;

        ApplyTheme();
        ApplyBackdrop();
        SkipToggleSwitchAnimations(this);
    }

    private void ComboBox_DropDownOpened(object sender, object e)
    {
        foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot))
        {
            if (popup.Child is FrameworkElement child)
            {
                child.RequestedTheme = RequestedTheme;
                UpdateThemeOfTooltips(child, RequestedTheme);
            }
        }
    }

    private void UiSettings_ColorValuesChanged(UISettings sender, object args)
    {
        _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        {
            if (ViewModel.Theme == 0)
                ApplyTheme();
        });
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainPageViewModel.Theme):
                ApplyTheme();
                break;
            case nameof(MainPageViewModel.Backdrop):
                ApplyBackdrop();
                ApplyBackground();
                break;
        }
    }

    private void ApplyTheme()
    {
        RequestedTheme = ViewModel.Theme switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => SystemElementTheme(),
        };

        TryUpdateApplicationTheme(RequestedTheme);
        ApplyBackground();
    }

    private void ApplyBackdrop()
    {
        if (HostWindow is null)
            return;

        HostWindow.SystemBackdrop = ViewModel.Backdrop switch
        {
            1 => new DesktopAcrylicBackdrop(),
            2 => new MicaBackdrop(),
            3 => new MicaBackdrop { Kind = MicaKind.BaseAlt },
            _ => null,
        };
    }

    private void ApplyBackground()
    {
        bool lightTheme = RequestedTheme != ElementTheme.Dark;

        if (IsMicaAvailable && ViewModel.Backdrop != 0)
        {
            Background = null;
            return;
        }

        Color backgroundColor = lightTheme ? Color.FromArgb(255, 243, 243, 243) : Color.FromArgb(255, 32, 32, 32);

        if (ViewModel.Backdrop == 1)
        {
            Background = new AcrylicBrush
            {
                BackgroundSource = AcrylicBackgroundSource.HostBackdrop,
                TintColor = lightTheme ? Color.FromArgb(255, 252, 252, 252) : Color.FromArgb(255, 44, 44, 44),
                TintOpacity = lightTheme ? 0.0 : 0.15,
                FallbackColor = backgroundColor,
            };
        }
        else
        {
            Background = new SolidColorBrush(backgroundColor);
        }
    }

    private static void TryUpdateApplicationTheme(ElementTheme theme)
    {
        try
        {
            WinUIIslands.Application.Current.RequestedTheme = theme == ElementTheme.Dark
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
        }
        catch
        {
        }
    }

    private ElementTheme SystemElementTheme()
    {
        var foreground = _uiSettings.GetColorValue(UIColorType.Foreground);
        return IsColorLight(foreground) ? ElementTheme.Dark : ElementTheme.Light;
    }

    private static bool IsColorLight(Color color) => 5 * color.G + 2 * color.R + color.B > 8 * 128;

    private static string LoadStringResource(string name, string fallback)
    {
        try
        {
            var value = ResourceLoader.GetForCurrentView().GetString(name);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }
        catch
        {
            return fallback;
        }
    }

    private static void SkipToggleSwitchAnimations(DependencyObject root)
    {
        Queue<DependencyObject> queue = new();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var count = VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(current, i);
                if (child is ToggleSwitch)
                    SkipToggleSwitchAnimation(child);
                else
                    queue.Enqueue(child);
            }
        }
    }

    private static void SkipToggleSwitchAnimation(DependencyObject toggleSwitch)
    {
        if (VisualTreeHelper.GetChildrenCount(toggleSwitch) == 0 ||
            VisualTreeHelper.GetChild(toggleSwitch, 0) is not FrameworkElement rootGrid)
        {
            return;
        }

        foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(rootGrid))
        {
            foreach (VisualState state in group.States)
                state.Storyboard?.SkipToFill();
        }
    }

    private static void UpdateThemeOfTooltips(DependencyObject root, ElementTheme theme)
    {
        Queue<DependencyObject> queue = new();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var tooltipContent = ToolTipService.GetToolTip(current);
            if (tooltipContent is ToolTip tooltip)
            {
                tooltip.RequestedTheme = theme;
            }
            else if (tooltipContent is not null)
            {
                ToolTip themedTooltip = new()
                {
                    Content = tooltipContent,
                    RequestedTheme = theme,
                };
                ToolTipService.SetToolTip(current, themedTooltip);
            }

            var count = VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < count; i++)
                queue.Enqueue(VisualTreeHelper.GetChild(current, i));
        }
    }

    private static unsafe uint QueryWindowsBuildNumber()
    {
        RTL_OSVERSIONINFOW version = new()
        {
            dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOW),
        };

        return RtlGetVersion(ref version) >= 0 ? version.dwBuildNumber : 0;
    }

    [LibraryImport("ntdll.dll")]
    private static partial int RtlGetVersion(ref RTL_OSVERSIONINFOW version);

    private unsafe struct RTL_OSVERSIONINFOW
    {
        public uint dwOSVersionInfoSize;
        public uint dwMajorVersion;
        public uint dwMinorVersion;
        public uint dwBuildNumber;
        public uint dwPlatformId;
        public fixed char szCSDVersion[128];
    }
}
