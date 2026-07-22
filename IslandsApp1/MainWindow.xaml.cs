using Windows.UI.Xaml;

namespace IslandsApp1
{
    public sealed partial class MainWindow : WinUIIslands.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }

        private void Button_Click(object sender, RoutedEventArgs e) =>
            TestButton.Content = "Clicked!";
    }
}
