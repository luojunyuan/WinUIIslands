using Windows.UI.Xaml;

namespace WinUIIslandsApp;

public sealed partial class MainWindow : WinUIIslands.Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private void Button_Click(object sender, RoutedEventArgs e) =>
        Title = Title == "WinUI Islands" ? "It works!" : "WinUI Islands";
}
