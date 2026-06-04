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
        public App()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override async void OnIslandLaunched(LaunchActivatedEventArgs e)
        {
            var mainBtn = new Button() { Content = "Click me" };
            mainBtn.Click += (_, _) =>
            {
                var window = new CoreIsland.Window()
                {
                    Title = "App1",
                    Content = new MainPage(),
                };

                var notepad = Process.Start("notepad");
                notepad.WaitForInputIdle();

                var child = CoreIsland.Windowing.WindowNative.GetWindowHandle(window);
                // window.SetParent(child, notepad.MainWindowHandle);

                window.Activate();
            };

            //mainBtn.Click += (_, _) =>
            //{
            //    var notepad = Process.Start("notepad");
            //    notepad.WaitForInputIdle();
            //    var mainHandle = notepad.MainWindowHandle;

            //    var win2 = new CoreIsland.Window(mainHandle)
            //    {
            //        Content = new Border
            //        {
            //            BorderBrush = new SolidColorBrush(Colors.Blue),
            //            BorderThickness = new Thickness(1),
            //        }
            //    };

            //    var win1 = new CoreIsland.Window(mainHandle)
            //    {
            //        Content = new Border
            //        {
            //            BorderBrush = new SolidColorBrush(Colors.Red),
            //            BorderThickness = new Thickness(2),
            //        }
            //    };

            //    win1.ActivateAsChild();

            //    win2.ActivateAsChild();

            //};

            var window = new CoreIsland.Window()
            {
                Content = mainBtn,
                SystemBackdrop = new CoreIsland.MicaBackdrop()
            };
            window.Activate();
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
