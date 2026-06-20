using CoreIsland.TitleBar;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using DrawingPoint = System.Drawing.Point;

namespace CoreIsland;

public unsafe partial class Window
{
    private const int HTNOWHERE = 0;
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTMINBUTTON = 8;
    private const int HTMAXBUTTON = 9;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;
    private const int HTCLOSE = 20;

    private const int SC_RESTORE = 0xF120;
    private const int SC_MOVE = 0xF010;
    private const int SC_SIZE = 0xF000;
    private const int SC_MINIMIZE = 0xF020;
    private const int SC_MAXIMIZE = 0xF030;
    private const int SC_CLOSE = 0xF060;

    private const int AutoHideTaskbarInset = 2;
    private const uint UINT_MAX = 0xffffffff;

    private UIElement? _titleBar;
    private CaptionButtonsControl? _captionButtons;
    private bool _isMaximized;
    private uint _currentDpi = PInvoke.USER_DEFAULT_SCREEN_DPI;
    private uint _nativeBorderThickness = 1;

    public bool ExtendsContentIntoTitleBar
    {
        get => field;
        set
        {
            if (field == value)
                return;

            field = value;
            ApplyTitleBarMode();
        }
    }

    public void SetTitleBar(UIElement? titleBar)
    {
        if (_titleBar is FrameworkElement oldElement)
            oldElement.SizeChanged -= TitleBar_SizeChanged;

        _titleBar = titleBar;

        if (_titleBar is FrameworkElement newElement)
            newElement.SizeChanged += TitleBar_SizeChanged;

        UpdateTitleBarWindow();
    }

    internal void SetCaptionButtons(CaptionButtonsControl? captionButtons)
    {
        if (_captionButtons is not null)
        {
            _captionButtons.SizeChanged -= CaptionButtons_SizeChanged;
            _captionButtons.Window = null;
        }

        _captionButtons = captionButtons;

        if (_captionButtons is not null)
        {
            _captionButtons.SizeChanged += CaptionButtons_SizeChanged;
            _captionButtons.Window = this;
        }

        UpdateTitleBarWindow();
    }

    private void ApplyTitleBarMode()
    {
        if (!ExtendsContentIntoTitleBar)
        {
            _captionButtons?.LeaveButtons();
            if (!_titleBarHwnd.IsNull)
            {
                PInvoke.SetWindowRgn(_titleBarHwnd, default, true);
                PInvoke.ShowWindow(_titleBarHwnd, SHOW_WINDOW_CMD.SW_HIDE);
            }
        }

        UpdateFrameMargins();

        PInvoke.SetWindowPos(_hwnd, default, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);

        UpdateXamlIslandBounds();
        UpdateTitleBarWindow();
    }

    private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleBarWindow();

    private void CaptionButtons_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleBarWindow();

    private LRESULT TitleBarWndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_NCHITTEST:
                return TryHitTestTitleBar(lParam, out var hit) ? hit : new LRESULT(HTCLIENT);

            case PInvoke.WM_NCMOUSEMOVE:
                HandleTitleBarMouseMove(wParam);
                return PInvoke.SendMessage(_hwnd, msg, wParam, lParam);

            case PInvoke.WM_NCLBUTTONDOWN:
                HandleTitleBarButtonDown(wParam);
                if (IsCaptionButtonHit(wParam))
                    return default;
                return PInvoke.SendMessage(_hwnd, msg, wParam, lParam);

            case PInvoke.WM_NCLBUTTONDBLCLK:
                if (IsCaptionButtonHit(wParam))
                    return default;
                return PInvoke.SendMessage(_hwnd, msg, wParam, lParam);

            case PInvoke.WM_NCLBUTTONUP:
                HandleTitleBarButtonUp(wParam);
                if (IsCaptionButtonHit(wParam))
                    return default;
                return PInvoke.SendMessage(_hwnd, msg, wParam, lParam);

            case PInvoke.WM_NCRBUTTONDOWN:
            case PInvoke.WM_NCRBUTTONDBLCLK:
            case PInvoke.WM_NCRBUTTONUP:
                return PInvoke.SendMessage(_hwnd, msg, wParam, lParam);
        }

        return PInvoke.DefWindowProc(_titleBarHwnd, msg, wParam, lParam);
    }

    private bool TryHandleNcCalcSize(WPARAM wParam, LPARAM lParam, out LRESULT result)
    {
        result = default;

        if (wParam.Value == 0)
            return true;

        var parameters = (NCCALCSIZE_PARAMS*)lParam.Value;
        var rects = parameters->rgrc.AsSpan();
        ref RECT clientRect = ref rects[0];
        var originalTop = clientRect.top;

        result = PInvoke.DefWindowProc(_hwnd, PInvoke.WM_NCCALCSIZE, wParam, lParam);
        if (result.Value != 0)
            return true;

        clientRect.top = originalTop;
        _isMaximized = PInvoke.IsZoomed(_hwnd);

        if (_isMaximized)
        {
            clientRect.top += GetResizeHandleHeight();
            ApplyAutoHideTaskbarInsets(ref clientRect);
        }

        UpdateFrameMargins();
        return true;
    }

    private bool TryHitTestTitleBar(LPARAM lParam, out LRESULT result)
    {
        result = default;

        POINT screenPoint = new() { x = GetXLParam(lParam), y = GetYLParam(lParam) };
        if (!TryGetClientRectInScreen(out var clientRect, out var clientTopLeft))
            return false;

        if (!Contains(clientRect, screenPoint))
            return false;

        if (screenPoint.y >= clientRect.top + GetTopBorderThickness() && !_titleBarHwnd.IsNull)
        {
            PInvoke.GetWindowRect(_titleBarHwnd, out var titleBarWindowRect);
            if (!Contains(titleBarWindowRect, screenPoint))
            {
                result = new LRESULT(HTCLIENT);
                return true;
            }
        }

        if (!_isMaximized)
        {
            var resizeHandleHeight = GetResizeHandleHeight();
            if (screenPoint.y < clientRect.top + resizeHandleHeight)
            {
                result = screenPoint.x < clientRect.left + resizeHandleHeight
                    ? new LRESULT(HTTOPLEFT)
                    : screenPoint.x >= clientRect.right - resizeHandleHeight
                        ? new LRESULT(HTTOPRIGHT)
                        : new LRESULT(HTTOP);
                return true;
            }
        }

        if (TryHitTestCaptionButtons(screenPoint, clientTopLeft, out var captionButton))
        {
            result = new LRESULT((int)captionButton);
            return true;
        }

        if (!TryGetElementBoundsInPixels(_titleBar, out var titleBarRect))
        {
            result = new LRESULT(HTCLIENT);
            return true;
        }

        titleBarRect = titleBarRect.Offset(clientTopLeft.X, clientTopLeft.Y + GetTopBorderThickness());
        result = Contains(titleBarRect, screenPoint) ? new LRESULT(HTCAPTION) : new LRESULT(HTCLIENT);
        return true;
    }

    private void UpdateTitleBarWindow()
    {
        if (_titleBarHwnd.IsNull)
            return;

        if (!ExtendsContentIntoTitleBar)
        {
            PInvoke.ShowWindow(_titleBarHwnd, SHOW_WINDOW_CMD.SW_HIDE);
            return;
        }

        if (!TryGetElementBoundsInPixels(_titleBar, out var titleBarBounds) &&
            !TryGetElementBoundsInPixels(_captionButtons, out titleBarBounds))
        {
            PInvoke.SetWindowRgn(_titleBarHwnd, default, true);
            PInvoke.ShowWindow(_titleBarHwnd, SHOW_WINDOW_CMD.SW_HIDE);
            return;
        }

        PInvoke.GetClientRect(_hwnd, out var clientRect);
        var topBorderThickness = GetTopBorderThickness();
        var height = topBorderThickness + Math.Max(1, titleBarBounds.Y + titleBarBounds.Height + 1);

        PInvoke.SetWindowPos(_titleBarHwnd, new HWND((nint)0), 0, 0, clientRect.Width, height,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);

        UpdateTitleBarWindowStyle();
        UpdateMaximizeButtonWindow();
        ApplyTitleBarWindowRegion(topBorderThickness);
        _captionButtons?.IsWindowMaximized(_isMaximized);
    }

    private void UpdateXamlIslandBounds()
    {
        if (_xamlHwnd.IsNull)
            return;

        PInvoke.GetClientRect(_hwnd, out var cr);
        UpdateXamlIslandBounds(cr);
    }

    private void UpdateXamlIslandBounds(RECT cr)
    {
        var topBorderThickness = GetTopBorderThickness();
        PInvoke.SetWindowPos(_xamlHwnd, default, cr.X, cr.Y + topBorderThickness, cr.Width, Math.Max(0, cr.Height - topBorderThickness),
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    private void UpdateTitleBarWindowStyle()
    {
        var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(_titleBarHwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style = _isMaximized ? style | WINDOW_STYLE.WS_MAXIMIZE : style & ~WINDOW_STYLE.WS_MAXIMIZE;
        PInvoke.SetWindowLongAnyCPU(_titleBarHwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);
    }

    private void UpdateMaximizeButtonWindow()
    {
        if (_maximizeButtonHwnd.IsNull || _captionButtons is null)
            return;

        if (!TryGetElementBoundsInPixels(_captionButtons.MaximizeButtonElement, out var bounds))
            return;

        PInvoke.SetWindowPos(_maximizeButtonHwnd, default, bounds.X, GetTopBorderThickness() + bounds.Y, bounds.Width, bounds.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    private void ApplyTitleBarWindowRegion(int topBorderThickness)
    {
        HRGN region = HRGN.Null;
        bool ownsRegion = true;

        try
        {
            AddElementToRegion(_titleBar, topBorderThickness, ref region);
            AddElementToRegion(_captionButtons, topBorderThickness, ref region);

            if (region.IsNull)
            {
                PInvoke.SetWindowRgn(_titleBarHwnd, default, true);
                return;
            }

            if (PInvoke.SetWindowRgn(_titleBarHwnd, region, true) != 0)
                ownsRegion = false;
        }
        finally
        {
            if (ownsRegion && !region.IsNull)
                PInvoke.DeleteObject(region);
        }
    }

    private void AddElementToRegion(UIElement? element, int topBorderThickness, ref HRGN region)
    {
        if (!TryGetElementBoundsInPixels(element, out var bounds))
            return;

        var elementRegion = PInvoke.CreateRectRgn(bounds.X, topBorderThickness + bounds.Y, bounds.X + bounds.Width, topBorderThickness + bounds.Y + bounds.Height);
        if (elementRegion.IsNull)
            return;

        if (region.IsNull)
        {
            region = elementRegion;
            return;
        }

        PInvoke.CombineRgn(region, region, elementRegion, RGN_COMBINE_MODE.RGN_OR);
        PInvoke.DeleteObject(elementRegion);
    }

    private bool TryGetElementBoundsInPixels(UIElement? element, out PixelRect bounds)
    {
        bounds = default;

        if (element is not FrameworkElement frameworkElement || Content is null)
            return false;

        var logicalBounds = new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight);
        if (logicalBounds.Width <= 0 || logicalBounds.Height <= 0)
            return false;

        try
        {
            logicalBounds = frameworkElement.TransformToVisual(Content).TransformBounds(logicalBounds);
        }
        catch
        {
            return false;
        }

        var scale = _currentDpi / (double)PInvoke.USER_DEFAULT_SCREEN_DPI;
        bounds = new PixelRect(
            (int)Math.Floor(logicalBounds.X * scale),
            (int)Math.Floor(logicalBounds.Y * scale),
            (int)Math.Ceiling(logicalBounds.Width * scale),
            (int)Math.Ceiling(logicalBounds.Height * scale));
        return true;
    }

    private int GetTopBorderThickness() => ExtendsContentIntoTitleBar && !_isMaximized ? (int)_nativeBorderThickness : 0;

    private int GetResizeHandleHeight()
    {
        return PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, _currentDpi) +
               PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME, _currentDpi);
    }

    private void UpdateDpi(uint dpi)
    {
        _currentDpi = dpi == 0 ? PInvoke.USER_DEFAULT_SCREEN_DPI : dpi;
        _nativeBorderThickness = 1;

        if (WindowsVersion.IsWin11OrNewer)
        {
            uint thickness = 0;
            var thicknessSpan = new Span<byte>(&thickness, sizeof(uint));
            if (PInvoke.DwmGetWindowAttribute(_hwnd, DWMWINDOWATTRIBUTE.DWMWA_VISIBLE_FRAME_BORDER_THICKNESS, thicknessSpan).Succeeded)
                _nativeBorderThickness = Math.Max(1, thickness);
        }
    }

    private void UpdateFrameMargins()
    {
        if (WindowsVersion.IsWin11OrNewer || _hwnd.IsNull)
            return;

        MARGINS margins = default;
        if (GetTopBorderThickness() > 0)
        {
            RECT frame = default;
            var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            PInvoke.AdjustWindowRectExForDpi(ref frame, style, false, exStyle, _currentDpi);
            margins.cyTopHeight = -frame.top;
        }

        PInvoke.DwmExtendFrameIntoClientArea(_hwnd, in margins);
    }

    private bool TryHandlePaint()
    {
        if (SystemBackdrop is not null && WindowsVersion.Is22H2OrNewer)
            return false;

        var hdc = PInvoke.BeginPaint(_hwnd, out var ps);
        if (hdc.IsNull)
            return true;

        try
        {
            var topBorderThickness = WindowsVersion.IsWin11OrNewer ? 0 : GetTopBorderThickness();
            if (ps.rcPaint.top < topBorderThickness)
            {
                RECT topBorder = ps.rcPaint;
                topBorder.bottom = topBorderThickness;
                var borderBrush = PInvoke.CreateSolidBrush(new COLORREF(0));
                if (!borderBrush.IsNull)
                {
                    PInvoke.FillRect(hdc, &topBorder, borderBrush);
                    PInvoke.DeleteObject(borderBrush);
                }
            }

            if (ps.rcPaint.bottom > topBorderThickness)
            {
                RECT rest = ps.rcPaint;
                rest.top = topBorderThickness;
                var brush = PInvoke.CreateSolidBrush(new COLORREF(IsLightTheme() ? 0x00f3f3f3u : 0x00202020u));
                if (!brush.IsNull)
                {
                    PInvoke.FillRect(hdc, &rest, brush);
                    PInvoke.DeleteObject(brush);
                }
            }
        }
        finally
        {
            PInvoke.EndPaint(_hwnd, in ps);
        }

        return true;
    }

    private bool TryShowSystemMenu(LPARAM lParam)
    {
        POINT cursorPoint = new() { x = GetXLParam(lParam), y = GetYLParam(lParam) };
        if (!PInvoke.GetWindowRect(_titleBarHwnd, out var titleBarRect) || !Contains(titleBarRect, cursorPoint))
            return false;

        var systemMenu = PInvoke.GetSystemMenu(_hwnd, false);
        if (systemMenu.IsNull)
            return false;

        bool isMaximized = _isMaximized;
        SetSystemMenuItemState(systemMenu, SC_RESTORE, isMaximized);
        SetSystemMenuItemState(systemMenu, SC_MOVE, !isMaximized);
        SetSystemMenuItemState(systemMenu, SC_SIZE, !isMaximized);
        SetSystemMenuItemState(systemMenu, SC_MINIMIZE, true);
        SetSystemMenuItemState(systemMenu, SC_MAXIMIZE, !isMaximized);
        SetSystemMenuItemState(systemMenu, SC_CLOSE, true);
        PInvoke.SetMenuDefaultItem(systemMenu, UINT_MAX, 0);

        var command = PInvoke.TrackPopupMenu(systemMenu, TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD, cursorPoint.x, cursorPoint.y, 0, _hwnd, null);
        if (command.Value == 0)
            return true;

        PInvoke.PostMessage(_hwnd, PInvoke.WM_SYSCOMMAND, new WPARAM((nuint)command.Value), default);
        return true;
    }

    private static void SetSystemMenuItemState(HMENU menu, int item, bool enabled)
    {
        MENUITEMINFOW menuItem = new()
        {
            cbSize = (uint)sizeof(MENUITEMINFOW),
            fMask = MENU_ITEM_MASK.MIIM_STATE,
            fType = MENU_ITEM_TYPE.MFT_STRING,
            fState = enabled ? MENU_ITEM_STATE.MFS_ENABLED : MENU_ITEM_STATE.MFS_DISABLED,
        };
        PInvoke.SetMenuItemInfo(menu, (uint)item, false, &menuItem);
    }

    private bool TryHandleGetTitleBarInfo(LPARAM lParam)
    {
        if (_maximizeButtonHwnd.IsNull)
            return false;

        var info = (TITLEBARINFOEX*)lParam.Value;
        if (info is null || info->cbSize < sizeof(TITLEBARINFOEX))
            return false;

        PInvoke.DefWindowProc(_hwnd, PInvoke.WM_GETTITLEBARINFOEX, default, lParam);
        if (PInvoke.GetWindowRect(_maximizeButtonHwnd, out var maximizeRect))
            info->rgrect.AsSpan()[3] = maximizeRect;

        return true;
    }

    private void HandleTitleBarMouseMove(WPARAM wParam)
    {
        if (_captionButtons is null || !TryCaptionButtonFromHitTest((int)wParam.Value, out var button))
        {
            _captionButtons?.LeaveButtons();
            return;
        }

        _captionButtons.HoverButton(button);
    }

    private void HandleTitleBarButtonDown(WPARAM wParam)
    {
        if (_captionButtons is not null && TryCaptionButtonFromHitTest((int)wParam.Value, out var button))
            _captionButtons.PressButton(button);
    }

    private void HandleTitleBarButtonUp(WPARAM wParam)
    {
        if (_captionButtons is not null && TryCaptionButtonFromHitTest((int)wParam.Value, out var button))
            _captionButtons.ReleaseButton(button);
        else
            _captionButtons?.ReleaseButtons();
    }

    private bool IsCaptionButtonHit(WPARAM wParam) => TryCaptionButtonFromHitTest((int)wParam.Value, out _);

    private static bool TryCaptionButtonFromHitTest(int hitTest, out CaptionButton button)
    {
        button = hitTest switch
        {
            HTMINBUTTON => CaptionButton.Minimize,
            HTMAXBUTTON => CaptionButton.Maximize,
            HTCLOSE => CaptionButton.Close,
            _ => default,
        };

        return button is CaptionButton.Minimize or CaptionButton.Maximize or CaptionButton.Close;
    }

    private bool TryHitTestCaptionButtons(POINT screenPoint, DrawingPoint clientTopLeft, out CaptionButton button)
    {
        button = default;

        if (_captionButtons is null || !TryGetElementBoundsInPixels(_captionButtons, out var bounds))
            return false;

        var rect = bounds.Offset(clientTopLeft.X, clientTopLeft.Y + GetTopBorderThickness());
        if (!Contains(rect, screenPoint))
            return false;

        var buttonWidth = Math.Max(1, rect.Width / 3);
        if (screenPoint.x < rect.X + buttonWidth)
            button = CaptionButton.Minimize;
        else if (screenPoint.x < rect.X + buttonWidth * 2)
            button = CaptionButton.Maximize;
        else
            button = CaptionButton.Close;

        return true;
    }

    private void ApplyAutoHideTaskbarInsets(ref RECT clientRect)
    {
        var monitor = PInvoke.MonitorFromWindow(_hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        MONITORINFO monitorInfo = new()
        {
            cbSize = (uint)sizeof(MONITORINFO),
        };

        if (monitor.IsNull || !PInvoke.GetMonitorInfo(monitor, ref monitorInfo) || !ShellAppBar.HasAnyAutoHideTaskbar())
            return;

        if (ShellAppBar.HasTopAutoHideTaskbar(monitorInfo.rcMonitor))
            clientRect.top += AutoHideTaskbarInset;
        if (ShellAppBar.HasBottomAutoHideTaskbar(monitorInfo.rcMonitor))
            clientRect.bottom -= AutoHideTaskbarInset;
        if (ShellAppBar.HasLeftAutoHideTaskbar(monitorInfo.rcMonitor))
            clientRect.left += AutoHideTaskbarInset;
        if (ShellAppBar.HasRightAutoHideTaskbar(monitorInfo.rcMonitor))
            clientRect.right -= AutoHideTaskbarInset;
    }

    private bool TryGetClientRectInScreen(out RECT rect, out DrawingPoint topLeft)
    {
        topLeft = default;
        if (!PInvoke.GetClientRect(_hwnd, out rect))
            return false;

        PInvoke.ClientToScreen(_hwnd, ref topLeft);
        rect.left += topLeft.X;
        rect.right += topLeft.X;
        rect.top += topLeft.Y;
        rect.bottom += topLeft.Y;
        return true;
    }

    private void RepositionXamlPopups()
    {
        if (Content is not FrameworkElement frameworkElement || frameworkElement.XamlRoot is null)
            return;

        _ = frameworkElement.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
        {
            foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(frameworkElement.XamlRoot))
            {
                var childName = popup.Child?.GetType().FullName;
                if (childName is "Windows.UI.Xaml.Controls.Primitives.FlyoutPresenter" or "Windows.UI.Xaml.Controls.Primitives.MenuFlyoutPresenter")
                {
                    popup.IsOpen = false;
                    continue;
                }

                var compositeMode = popup.CompositeMode;
                popup.CompositeMode = compositeMode == ElementCompositeMode.SourceOver
                    ? ElementCompositeMode.MinBlend
                    : ElementCompositeMode.SourceOver;
                popup.CompositeMode = compositeMode;
            }
        });
    }

    private static bool Contains(RECT rect, POINT point) =>
        point.x >= rect.left && point.x < rect.right && point.y >= rect.top && point.y < rect.bottom;

    private static bool Contains(PixelRect rect, POINT point) =>
        point.x >= rect.X && point.x < rect.X + rect.Width && point.y >= rect.Y && point.y < rect.Y + rect.Height;

    private static int GetXLParam(LPARAM lParam) => unchecked((short)(lParam.Value & 0xffff));

    private static int GetYLParam(LPARAM lParam) => unchecked((short)((lParam.Value >> 16) & 0xffff));

    private static bool IsLightTheme() => Application.Current.RequestedTheme != Windows.UI.Xaml.ApplicationTheme.Dark;

    private readonly record struct PixelRect(int X, int Y, int Width, int Height)
    {
        public PixelRect Offset(int x, int y) => new(X + x, Y + y, Width, Height);
    }
}
