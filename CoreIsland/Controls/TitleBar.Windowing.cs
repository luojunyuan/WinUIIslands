// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace CoreIsland.Controls;

public partial class TitleBar
{
    private const int HtNowhere = 0;
    private const int HtClient = 1;
    private const int HtCaption = 2;
    private const int HtSystemMenu = 3;

    private readonly List<FrameworkElement> _interactableElements = [];
    private readonly List<PixelRect> _previousPassthroughRects = [];

    private CoreIsland.Window? _window;
    private string _defaultWindowTitle = string.Empty;
    private bool _hasDefaultWindowTitle;

    internal void AttachWindow(CoreIsland.Window window)
    {
        if (ReferenceEquals(_window, window))
        {
            RefreshWindowInsets();
            UpdateDragRegion(force: true);
            return;
        }

        if (_window is not null)
            DetachWindow(_window);

        _window = window;
        _window.Activated += OnWindowActivated;
        _window.Closed += OnWindowClosed;

        UpdatePadding();
        UpdateTitle();
        UpdateActivationStates();
        UpdateInteractableElementsList();
        UpdateDragRegion(force: true);
    }

    internal void DetachWindow(CoreIsland.Window window)
    {
        if (!ReferenceEquals(_window, window))
            return;

        _window.Activated -= OnWindowActivated;
        _window.Closed -= OnWindowClosed;
        ResetWindowTitle(Title);
        _window = null;
        _hasDefaultWindowTitle = false;
        _defaultWindowTitle = string.Empty;
        UpdatePadding();
    }

    internal void RefreshWindowInsets()
    {
        UpdatePadding();
        UpdateDragRegion(force: true);
    }

    internal int HitTest(
        int screenX,
        int screenY,
        int xamlRootScreenX,
        int xamlRootScreenY)
    {
        var point = new PixelPoint(screenX - xamlRootScreenX, screenY - xamlRootScreenY);

        if (IconSource is not null && _iconViewbox is not null &&
            TryGetBounds(_iconViewbox, out var iconRect) && iconRect.Contains(point))
        {
            return HtSystemMenu;
        }

        foreach (var rect in GetPassthroughRects())
        {
            if (rect.Contains(point))
                return HtClient;
        }

        return TryGetBounds(this, out var titleBarRect) && titleBarRect.Contains(point)
            ? HtCaption
            : HtNowhere;
    }

    internal bool ApplyTitleBarWindowRegion(
        long titleBarWindowHandle,
        int xamlRootScreenX,
        int xamlRootScreenY)
    {
        if (!TryGetBounds(this, out var titleBarRect) || titleBarRect.Width <= 0 || titleBarRect.Height <= 0)
            return false;

        var hwnd = new HWND((nint)titleBarWindowHandle);
        if (hwnd.IsNull || !PInvoke.GetWindowRect(hwnd, out var windowRect))
            return false;

        var offsetX = xamlRootScreenX - windowRect.left;
        var offsetY = xamlRootScreenY - windowRect.top;
        HRGN region = HRGN.Null;
        bool ownsRegion = true;

        try
        {
            region = CreateRegion(titleBarRect.Offset(offsetX, offsetY));
            if (region.IsNull)
                return false;

            foreach (var passthroughRect in GetPassthroughRects())
            {
                var hole = CreateRegion(passthroughRect.Offset(offsetX, offsetY));
                if (hole.IsNull)
                    continue;

                PInvoke.CombineRgn(region, region, hole, RGN_COMBINE_MODE.RGN_DIFF);
                PInvoke.DeleteObject(hole);
            }

            if (PInvoke.SetWindowRgn(hwnd, region, true) == 0)
                return false;

            ownsRegion = false;
            return true;
        }
        finally
        {
            if (ownsRegion && !region.IsNull)
                PInvoke.DeleteObject(region);
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        _isWindowActive = args.IsActive;
        UpdateActivationStates();
        UpdateDragRegion(force: true);
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        if (_window is null)
            return;

        _window.Activated -= OnWindowActivated;
        _window.Closed -= OnWindowClosed;
        _window = null;
        _hasDefaultWindowTitle = false;
        _defaultWindowTitle = string.Empty;
    }

    private void UpdateWindowTitle()
    {
        if (_window is null)
            return;

        if (!_hasDefaultWindowTitle)
        {
            _defaultWindowTitle = _window.Title;
            _hasDefaultWindowTitle = true;
        }

        if (!string.IsNullOrEmpty(Title) && _window.Title != Title)
            _window.Title = Title;
    }

    private void ResetWindowTitle(string lastAppliedTitle)
    {
        if (_window is null || !_hasDefaultWindowTitle)
            return;

        if (_window.Title == lastAppliedTitle && _window.Title != _defaultWindowTitle)
        {
            _window.Title = _defaultWindowTitle;
            _hasDefaultWindowTitle = false;
        }
    }

    private void UpdateInteractableElementsList()
    {
        _interactableElements.Clear();

        if (IsBackButtonVisible && IsBackButtonEnabled && _backButton is not null)
            _interactableElements.Add(_backButton);

        if (IsPaneToggleButtonVisible && _paneToggleButton is not null)
            _interactableElements.Add(_paneToggleButton);

        if (LeftHeader is not null && _leftHeaderArea is not null)
            _interactableElements.Add(_leftHeaderArea);

        if (Content is not null && _contentArea is not null)
            FindInteractableElements(_contentArea, parentIsDragRegion: false);

        if (RightHeader is not null && _rightHeaderArea is not null)
            _interactableElements.Add(_rightHeaderArea);
    }

    private void FindInteractableElements(DependencyObject? element, bool parentIsDragRegion)
    {
        if (element is not UIElement uiElement ||
            uiElement.Visibility != Visibility.Visible ||
            !uiElement.IsHitTestVisible)
        {
            return;
        }

        var currentIsDragRegion = parentIsDragRegion;
        var isDragRegion = GetIsDragRegion(uiElement);

        if (isDragRegion.HasValue)
        {
            if (!isDragRegion.Value)
            {
                if (uiElement is FrameworkElement frameworkElement)
                    _interactableElements.Add(frameworkElement);
                return;
            }

            if (uiElement is Control)
                return;

            currentIsDragRegion = true;
        }

        if (!currentIsDragRegion && uiElement is Control control && control.IsEnabled)
        {
            _interactableElements.Add(control);
            return;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
            FindInteractableElements(VisualTreeHelper.GetChild(element, i), currentIsDragRegion);
    }

    private void UpdateDragRegion(bool force = false)
    {
        var passthroughRects = GetPassthroughRects();
        if (!force && passthroughRects.SequenceEqual(_previousPassthroughRects))
            return;

        _previousPassthroughRects.Clear();
        _previousPassthroughRects.AddRange(passthroughRects);
        _window?.RefreshTitleBarWindow();
    }

    private List<PixelRect> GetPassthroughRects()
    {
        List<PixelRect> rects = [];

        foreach (var element in _interactableElements)
        {
            if (element.Visibility == Visibility.Visible && element.IsHitTestVisible &&
                TryGetBounds(element, out var rect) && (rect.X >= 0 || rect.Y >= 0))
            {
                rects.Add(rect);
            }
        }

        return rects;
    }

    private bool TryGetBounds(FrameworkElement element, out PixelRect bounds)
    {
        bounds = default;

        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
            return false;

        try
        {
            var logicalBounds = element.TransformToVisual(null).TransformBounds(
                new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            var scale = XamlRoot?.RasterizationScale ?? 1.0;

            bounds = new PixelRect(
                (int)Math.Floor(logicalBounds.X * scale),
                (int)Math.Floor(logicalBounds.Y * scale),
                (int)Math.Ceiling(logicalBounds.Width * scale),
                (int)Math.Ceiling(logicalBounds.Height * scale));
            return bounds.Width > 0 && bounds.Height > 0;
        }
        catch
        {
            return false;
        }
    }

    private static HRGN CreateRegion(PixelRect rect) => PInvoke.CreateRectRgn(
        rect.X,
        rect.Y,
        rect.X + rect.Width,
        rect.Y + rect.Height);

    private readonly record struct PixelPoint(int X, int Y);

    private readonly record struct PixelRect(int X, int Y, int Width, int Height)
    {
        public bool Contains(PixelPoint point) =>
            point.X >= X && point.X < X + Width && point.Y >= Y && point.Y < Y + Height;

        public PixelRect Offset(int x, int y) => new(X + x, Y + y, Width, Height);
    }
}
