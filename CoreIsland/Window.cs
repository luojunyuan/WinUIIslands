using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Hosting;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using WinRT;

namespace CoreIsland;

public unsafe partial class Window
{
    private const string ClassName = "CoreIsland_Wnd";
    private const string TitleBarClassName = "CoreIsland_TitleBar_Wnd";
    private static readonly FreeLibrarySafeHandle s_hModule = PInvoke.GetModuleHandle();
    private static readonly WNDPROC s_wndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)&StaticWndProc;
    private static readonly WNDPROC s_titleBarWndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)&StaticTitleBarWndProc;

    static Window()
    {
        fixed (char* pClassName = ClassName)
        fixed (char* pTitleBarClassName = TitleBarClassName)
        {
            WNDCLASSEXW wc = new()
            {
                cbSize = (uint)sizeof(WNDCLASSEXW),
                lpfnWndProc = s_wndProc,
                hInstance = (HINSTANCE)s_hModule.DangerousGetHandle(),
                lpszClassName = pClassName,
            };

            if (PInvoke.RegisterClassEx(in wc) == 0)
                throw new Win32Exception();

            wc.lpfnWndProc = s_titleBarWndProc;
            wc.lpszClassName = pTitleBarClassName;

            if (PInvoke.RegisterClassEx(in wc) == 0)
                throw new Win32Exception();
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT StaticWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == PInvoke.WM_NCCREATE)
        {
            var cs = (CREATESTRUCTW*)lParam.Value;
            var pSelf = (nint)cs->lpCreateParams;
            PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, pSelf);

            if (GCHandle.FromIntPtr(pSelf).Target is Window self)
                self._hwnd = hwnd;
        }
        else
        {
            var userData = PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
            if (userData != 0 && GCHandle.FromIntPtr(userData).Target is Window self)
                return self.WndProc(msg, wParam, lParam);
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static LRESULT StaticTitleBarWndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        if (msg == PInvoke.WM_NCCREATE)
        {
            var cs = (CREATESTRUCTW*)lParam.Value;
            var pSelf = (nint)cs->lpCreateParams;
            PInvoke.SetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWL_USERDATA, pSelf);

            if (GCHandle.FromIntPtr(pSelf).Target is Window self)
                self._titleBarHwnd = hwnd;
        }
        else
        {
            var userData = PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
            if (userData != 0 && GCHandle.FromIntPtr(userData).Target is Window self)
                return self.TitleBarWndProc(msg, wParam, lParam);
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

public unsafe partial class Window
{
    private readonly DesktopWindowXamlSource _xamlHost = new();
    private readonly GCHandle _selfHandle;
    private readonly HWND _xamlHwnd;
    private HWND _hwnd;
    private HWND _titleBarHwnd;

    internal HWND Hwnd => _hwnd;

    public Window()
    {
        _selfHandle = GCHandle.Alloc(this);
        PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME,
            lpClassName: ClassName,
            lpWindowName: string.Empty,
            dwStyle: WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
            X: PInvoke.CW_USEDEFAULT,
            Y: PInvoke.CW_USEDEFAULT,
            nWidth: PInvoke.CW_USEDEFAULT,
            nHeight: PInvoke.CW_USEDEFAULT,
            hWndParent: default,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (_hwnd.IsNull)
            throw new Win32Exception();

        Application.Current.RegisterWindow(this);

        var nativeSource = _xamlHost.As<IDesktopWindowXamlSourceNative2>();
        nativeSource.AttachToWindow(_hwnd);
        nativeSource.GetWindowHandle(out _xamlHwnd);

        PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOPARENTNOTIFY |
                       WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_NOACTIVATE,
            lpClassName: TitleBarClassName,
            lpWindowName: string.Empty,
            dwStyle: WINDOW_STYLE.WS_CHILD,
            X: 0,
            Y: 0,
            nWidth: 0,
            nHeight: 0,
            hWndParent: _hwnd,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (_titleBarHwnd.IsNull)
            throw new Win32Exception();

        PInvoke.SetLayeredWindowAttributes(_titleBarHwnd, new COLORREF(0), 255, LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA);

        EnableResizeLayoutSynchronization(_hwnd, true);
    }

    private IFrameworkApplicationPrivate FrameworkAppPrivate { get; } = Windows.UI.Xaml.Application.Current.As<IFrameworkApplicationPrivate>();

    private LRESULT WndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_ACTIVATE when wParam.Value != 0:
                Application.Current.OnWindowActivated(this);
                return default;

            case PInvoke.WM_GETMINMAXINFO:
                HandleGetMinMaxInfo(lParam);
                return default;

            case PInvoke.WM_NCCALCSIZE:
                if (ExtendsContentIntoTitleBar && TryHandleNcCalcSize(wParam, lParam, out var ncCalcResult))
                    return ncCalcResult;
                break;

            case PInvoke.WM_NCHITTEST:
                if (ExtendsContentIntoTitleBar && TryHitTestTitleBar(lParam, out var hitTestResult))
                    return hitTestResult;
                break;

            case PInvoke.WM_SIZE when wParam.Value != PInvoke.SIZE_MINIMIZED:
                _isMaximized = PInvoke.IsZoomed(_hwnd);
                PInvoke.GetClientRect(_hwnd, out RECT cr);
                OnSizeChanged(cr.Width, cr.Height);
                var topBorderThickness = GetTopBorderThickness();
                PInvoke.SetWindowPos(_xamlHwnd, default, cr.X, cr.Y + topBorderThickness, cr.Width, cr.Height - topBorderThickness,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
                UpdateTitleBarWindow();

                PInvoke.SendMessage(Application.CoreHwnd, PInvoke.WM_SIZE, wParam, lParam);

                FrameworkAppPrivate.SetSynchronizationWindow(_hwnd);
                return default;

            case PInvoke.WM_DESTROY:
                if (!_titleBarHwnd.IsNull)
                {
                    PInvoke.DestroyWindow(_titleBarHwnd);
                    _titleBarHwnd = default;
                }
                _xamlHost?.Dispose();
                if (_selfHandle.IsAllocated)
                    _selfHandle.Free();
                Application.Current.OnWindowClosing(this);
                OnClosed();
                return default;
        }

        return PInvoke.DefWindowProc(_hwnd, msg, wParam, lParam);
    }

    [LibraryImport("user32.dll", EntryPoint = "#2615", SetLastError = false)]
    private static partial void EnableResizeLayoutSynchronization(HWND hwnd, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
