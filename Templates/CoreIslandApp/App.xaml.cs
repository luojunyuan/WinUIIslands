using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace CoreIslandApp;

public sealed partial class App : CoreIsland.Application
{
    private CoreIsland.Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnIslandLaunched(LaunchActivatedEventArgs e)
    {
        var rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;

        _window = new CoreIsland.Window()
        {
            Content = rootFrame
        };

        rootFrame.Navigate(typeof(MainPage), e.Arguments);

        _window.Activate();
    }

    private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception($"Failed to load Page {e.SourcePageType.FullName}");
    }
}
