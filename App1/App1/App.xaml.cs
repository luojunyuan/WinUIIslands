using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using CoreIsland.Windowing;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Navigation;

namespace App1
{
    public sealed partial class App : CoreIsland.Application
    {
        public App()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override async void OnIslandLaunched(LaunchActivatedEventArgs e)
        {
            var mainWindow = new MainWindow();
            //mainWindow.Content = new MainPage();
            mainWindow.Activate();
        }

        [LibraryImport("USER32.dll", SetLastError = false), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static partial nint SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        /// <summary>
        /// Invoked when Navigation to a certain page fails.
        /// </summary>
        /// <param name="sender">The Frame which failed navigation.</param>
        /// <param name="e">Details about the navigation failure.</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
