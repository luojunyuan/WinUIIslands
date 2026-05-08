using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CoreIslandApp;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        // Open a new CoreIsland window on button click
        var rootFrame = new Frame();
        var newWindow = new CoreIsland.Window()
        {
            Content = rootFrame
        };
        rootFrame.Navigate(typeof(MainPage));
        newWindow.Title = "CoreIsland Window";
        newWindow.Activate();
    }
}
