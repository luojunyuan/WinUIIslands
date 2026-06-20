using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CoreIsland.Windowing;
using Windows.ApplicationModel.Activation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace App1
{
    public sealed partial class App : CoreIsland.Application
    {
        private CoreIsland.Window? _mainWindow;

        public App()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override async void OnIslandLaunched(LaunchActivatedEventArgs e)
        {
            _mainWindow = new MainWindow();
            _mainWindow.Content = new MainPage(_mainWindow);
            _mainWindow.Activate();
        }
    }
}
