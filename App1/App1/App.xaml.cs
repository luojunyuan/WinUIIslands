using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        private CoreIsland.Window? _window;

        public App()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void OnIslandLaunched(LaunchActivatedEventArgs e)
        {
            var rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;

            _window = new CoreIsland.Window()
            {
                Content = rootFrame
            };

            nint hWnd = WindowNative.GetWindowHandle(_window);

            rootFrame.Navigate(typeof(MainPage), e.Arguments);

            if (_window.AppWindow?.Presenter is CoreIsland.Windowing.OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
            }

            var notepad = Process.Start("notepad");
            notepad!.WaitForInputIdle();
            var p = notepad.MainWindowHandle;

            SetParent(hWnd, p);

            _window.Activate();
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
