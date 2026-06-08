using System;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using CoreIsland.TitleBar;

namespace App1
{
    /// <summary>
    /// A page that displays scrolling danmaku text using Composition animations.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Compositor _compositor;
        private readonly List<bool> _trackOccupied = new();

        private const double TrackHeight = 32;
        private const double DefaultSpeed = 200; // pixels per second
        private const double FontSizeSmall = 16;
        private const double FontSizeLarge = 28;
        public TitleBarControl CustomTitleBar => (TitleBarControl)FindName("TitleBarRoot");

        public MainPage()
        {
            InitializeComponent();
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            SizeChanged += OnPageSizeChanged;
        }

        private void OnPageSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Recalculate available tracks when the page resizes
            var trackCount = (int)(e.NewSize.Height / TrackHeight);
            while (_trackOccupied.Count < trackCount)
            {
                _trackOccupied.Add(false);
            }
            while (_trackOccupied.Count > trackCount)
            {
                _trackOccupied.RemoveAt(_trackOccupied.Count - 1);
            }
        }

        /// <summary>
        /// Sends a danmaku bullet comment that scrolls from right to left.
        /// </summary>
        public void SendDanmaku(string text, string color = "#FFFFFF", double fontSize = FontSizeSmall, double speed = DefaultSpeed)
        {
            if (string.IsNullOrWhiteSpace(text) || DanmakuCanvas.ActualWidth <= 0)
                return;

            // Create the TextBlock
            var tb = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                Foreground = new SolidColorBrush(ParseColor(color)),
                TextWrapping = TextWrapping.NoWrap,
            };

            // Measure text width
            tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            var textWidth = tb.DesiredSize.Width;

            // Place above the input bar if text would extend below it
            // Add to canvas so it can be measured properly
            DanmakuCanvas.Children.Add(tb);

            // Find an available track
            var trackIndex = FindAvailableTrack();
            if (trackIndex < 0)
            {
                // All tracks full, skip this one
                DanmakuCanvas.Children.Remove(tb);
                return;
            }

            // Position the text at the right edge of the canvas
            var startX = DanmakuCanvas.ActualWidth;
            var endX = -textWidth;
            var y = trackIndex * TrackHeight;
            Canvas.SetLeft(tb, startX);
            Canvas.SetTop(tb, y);

            // Get the Visual for Composition animation
            var visual = ElementCompositionPreview.GetElementVisual(tb);

            // Calculate duration based on speed
            var distance = DanmakuCanvas.ActualWidth + textWidth;
            var duration = TimeSpan.FromSeconds(distance / speed);

            // --- Composition Animation ---
            var batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

            var animation = _compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0.0f, (float)startX);
            animation.InsertKeyFrame(1.0f, (float)endX);
            animation.Duration = duration;

            visual.StartAnimation("Translation.X", animation);

            batch.End();

            // --- Cleanup when animation completes ---
            batch.Completed += (sender, args) =>
            {
                if (trackIndex < _trackOccupied.Count)
                {
                    _trackOccupied[trackIndex] = false;
                }
                DanmakuCanvas.Children.Remove(tb);
            };
        }

        /// <summary>
        /// Finds the first unoccupied track index.
        /// Returns -1 if all tracks are occupied.
        /// </summary>
        private int FindAvailableTrack()
        {
            for (int i = 0; i < _trackOccupied.Count; i++)
            {
                if (!_trackOccupied[i])
                {
                    _trackOccupied[i] = true;
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Parses a hex color string (e.g. "#FF0000" or "FF0000") to a Windows.UI.Color.
        /// </summary>
        private static Color ParseColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color))
                return Colors.White;

            var hex = color.TrimStart('#');
            if (hex.Length == 6)
            {
                hex = "FF" + hex; // fully opaque
            }

            if (hex.Length == 8 && uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var argb))
            {
                return Color.FromArgb(
                    (byte)((argb >> 24) & 0xFF),
                    (byte)((argb >> 16) & 0xFF),
                    (byte)((argb >> 8) & 0xFF),
                    (byte)(argb & 0xFF));
            }

            return Colors.White;
        }

        // --- Event Handlers ---

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            SendDanmaku(InputTextBox.Text);
            InputTextBox.Text = string.Empty;
        }

        private void InputTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                SendDanmaku(InputTextBox.Text);
                InputTextBox.Text = string.Empty;
                e.Handled = true;
            }
        }
    }
}
