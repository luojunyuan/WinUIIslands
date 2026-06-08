using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.WindowsAndMessaging;
using DrawingPoint = System.Drawing.Point;

namespace CoreIsland;

public unsafe partial class Window
{
    private const int HTCLIENT = 1;
    private const int HTCAPTION = 2;
    private const int HTTOP = 12;
    private const int HTTOPLEFT = 13;
    private const int HTTOPRIGHT = 14;

    private UIElement? _titleBar;
    private Rect _titleBarBounds;
    private bool _isMaximized;

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

    private void ApplyTitleBarMode()
    {
        if (!ExtendsContentIntoTitleBar)
        {
            PInvoke.ShowWindow(_titleBarHwnd, SHOW_WINDOW_CMD.SW_HIDE);
        }

        MARGINS margins = default;
        PInvoke.DwmExtendFrameIntoClientArea(_hwnd, in margins);

        PInvoke.SetWindowPos(_hwnd, default, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);

        UpdateXamlIslandBounds();
        UpdateTitleBarWindow();
    }

    private void TitleBar_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateTitleBarWindow();

    private LRESULT TitleBarWndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_NCHITTEST:
                return TryHitTestTitleBar(lParam, out var hit) ? hit : new LRESULT(HTCLIENT);

            case PInvoke.WM_NCLBUTTONDOWN:
            case PInvoke.WM_NCLBUTTONDBLCLK:
            case PInvoke.WM_NCLBUTTONUP:
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
        var originalTop = rects[0].top;

        result = PInvoke.DefWindowProc(_hwnd, PInvoke.WM_NCCALCSIZE, wParam, lParam);
        if (result.Value != 0)
            return true;

        _isMaximized = PInvoke.IsZoomed(_hwnd);

        rects[0].top = originalTop;

        if (_isMaximized)
            rects[0].top += GetResizeHandleHeight();

        return true;
    }

    private bool TryHitTestTitleBar(LPARAM lParam, out LRESULT result)
    {
        result = default;

        POINT screenPoint = new() { x = GetXLParam(lParam), y = GetYLParam(lParam) };
        PInvoke.GetClientRect(_hwnd, out var clientRect);
        DrawingPoint clientTopLeft = default;
        PInvoke.ClientToScreen(_hwnd, ref clientTopLeft);
        clientRect.left += clientTopLeft.X;
        clientRect.right += clientTopLeft.X;
        clientRect.top += clientTopLeft.Y;
        clientRect.bottom += clientTopLeft.Y;

        if (!Contains(clientRect, screenPoint))
            return false;

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

        if (!TryGetTitleBarBoundsInPixels(out var titleBarRect))
        {
            result = new LRESULT(HTCLIENT);
            return true;
        }

        titleBarRect = titleBarRect.Offset(clientTopLeft.X, clientTopLeft.Y + GetTopBorderThickness());

        if (screenPoint.x >= titleBarRect.X && screenPoint.x < titleBarRect.X + titleBarRect.Width &&
            screenPoint.y >= titleBarRect.Y && screenPoint.y < titleBarRect.Y + titleBarRect.Height)
        {
            result = new LRESULT(HTCAPTION);
            return true;
        }

        result = new LRESULT(HTCLIENT);
        return true;
    }

    private void UpdateTitleBarWindow()
    {
        if (_titleBarHwnd.IsNull)
            return;

        if (!ExtendsContentIntoTitleBar || !TryGetTitleBarBoundsInPixels(out var bounds))
        {
            PInvoke.ShowWindow(_titleBarHwnd, SHOW_WINDOW_CMD.SW_HIDE);
            return;
        }

        var topBorderThickness = GetTopBorderThickness();

        PInvoke.SetWindowPos(_titleBarHwnd, default, bounds.X, topBorderThickness + bounds.Y, bounds.Width, bounds.Height + 1,
            SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    private void UpdateXamlIslandBounds()
    {
        if (_xamlHwnd.IsNull)
            return;

        PInvoke.GetClientRect(_hwnd, out var cr);
        var topBorderThickness = GetTopBorderThickness();
        PInvoke.SetWindowPos(_xamlHwnd, default, cr.X, cr.Y + topBorderThickness, cr.Width, cr.Height - topBorderThickness,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
    }

    private bool TryGetTitleBarBoundsInPixels(out PixelRect bounds)
    {
        bounds = default;

        if (_titleBar is not FrameworkElement titleBar || Content is null)
            return false;

        var logicalBounds = new Rect(0, 0, titleBar.ActualWidth, titleBar.ActualHeight);
        if (logicalBounds.Width <= 0 || logicalBounds.Height <= 0)
            return false;

        _titleBarBounds = titleBar.TransformToVisual(Content).TransformBounds(logicalBounds);
        var dpi = PInvoke.GetDpiForWindow(_hwnd);
        var scale = dpi / (double)PInvoke.USER_DEFAULT_SCREEN_DPI;

        bounds = new PixelRect(
            (int)Math.Floor(_titleBarBounds.X * scale),
            (int)Math.Floor(_titleBarBounds.Y * scale),
            (int)Math.Ceiling(_titleBarBounds.Width * scale),
            (int)Math.Ceiling(_titleBarBounds.Height * scale));
        return true;
    }

    private int GetTopBorderThickness() => 0;

    private int GetResizeHandleHeight()
    {
        var dpi = PInvoke.GetDpiForWindow(_hwnd);
        return PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CXPADDEDBORDER, dpi) +
               PInvoke.GetSystemMetricsForDpi(SYSTEM_METRICS_INDEX.SM_CYSIZEFRAME, dpi);
    }

    private static bool Contains(RECT rect, POINT point) =>
        point.x >= rect.left && point.x < rect.right && point.y >= rect.top && point.y < rect.bottom;

    private static int GetXLParam(LPARAM lParam) => unchecked((short)(lParam.Value & 0xffff));

    private static int GetYLParam(LPARAM lParam) => unchecked((short)((lParam.Value >> 16) & 0xffff));

    private readonly record struct PixelRect(int X, int Y, int Width, int Height)
    {
        public PixelRect Offset(int x, int y) => new(X + x, Y + y, Width, Height);
    }
}
