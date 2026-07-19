using Windows.ApplicationModel.Activation;
namespace WinUIIslandsApp;

public sealed partial class App : WinUIIslands.Application
{
    private MainWindow? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnIslandLaunched(LaunchActivatedEventArgs e)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
