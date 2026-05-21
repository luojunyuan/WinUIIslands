using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland.Windowing;

public sealed class OverlappedPresenter : AppWindowPresenter
{
    public override AppWindowPresenterKind Kind => AppWindowPresenterKind.Overlapped;

    private bool _isResizable = true;
    private bool _isMaximizable = true;
    private bool _isMinimizable = true;
    private bool _hasBorder = true;
    private bool _hasTitleBar = true;

    public bool IsResizable
    {
        get => _isResizable;
        set { _isResizable = value; ApplyStyles(); }
    }

    public bool IsMaximizable
    {
        get => _isMaximizable;
        set { _isMaximizable = value; ApplyStyles(); }
    }

    public bool IsMinimizable
    {
        get => _isMinimizable;
        set { _isMinimizable = value; ApplyStyles(); }
    }

    public int PreferredMinimumWidth { get; set; }
    public int PreferredMinimumHeight { get; set; }

    internal OverlappedPresenter(AppWindow appWindow) : base(appWindow) { }

    public void AddChildStyle()
    {
        var hwnd = AppWindow.Hwnd;
        var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style |= WINDOW_STYLE.WS_CHILD;
        PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);
    }

    public void MakeClickThrough()
    {
        var hwnd = AppWindow.Hwnd;
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle |= WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_LAYERED;
        PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);
    }

    public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
    {
        _hasBorder = hasBorder;
        _hasTitleBar = hasTitleBar;
        ApplyStyles();
    }

    private void ApplyStyles()
    {
        var hwnd = AppWindow.Hwnd;
        var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);

        if (!_hasBorder)
        {
            style &= ~(WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU |
                        WINDOW_STYLE.WS_THICKFRAME | WINDOW_STYLE.WS_BORDER |
                        WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_MINIMIZEBOX);
        }
        else
        {
            if (_hasTitleBar)
            {
                style |= WINDOW_STYLE.WS_CAPTION;
                style |= WINDOW_STYLE.WS_SYSMENU;

                if (_isMaximizable)
                    style |= WINDOW_STYLE.WS_MAXIMIZEBOX;
                else
                    style &= ~WINDOW_STYLE.WS_MAXIMIZEBOX;

                if (_isMinimizable)
                    style |= WINDOW_STYLE.WS_MINIMIZEBOX;
                else
                    style &= ~WINDOW_STYLE.WS_MINIMIZEBOX;
            }
            else
            {
                style &= ~(WINDOW_STYLE.WS_CAPTION | WINDOW_STYLE.WS_SYSMENU |
                            WINDOW_STYLE.WS_MAXIMIZEBOX | WINDOW_STYLE.WS_MINIMIZEBOX);
            }

            if (_isResizable)
                style |= WINDOW_STYLE.WS_THICKFRAME;
            else
                style &= ~WINDOW_STYLE.WS_THICKFRAME;
        }

        PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);
        PInvoke.SetWindowPos(hwnd, default, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
            SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
    }
}
