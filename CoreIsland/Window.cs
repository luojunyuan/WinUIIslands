using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CoreIsland.TitleBar;
using Windows.UI.Xaml.Controls;
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
                hCursor = PInvoke.LoadCursor(default, PInvoke.IDC_ARROW),
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
    private readonly Grid _contentRoot = new();
    private readonly ContentPresenter _contentPresenter = new();
    private readonly CaptionButtonsControl _defaultCaptionButtons = new();
    private readonly GCHandle _selfHandle;
    private readonly HWND _xamlHwnd;
    private HWND _hwnd;
    private HWND _titleBarHwnd;
    private HWND _maximizeButtonHwnd;

    internal HWND Hwnd => _hwnd;

    public Window(nint hwndParent = default)
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
            hWndParent: new(hwndParent),
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (_hwnd.IsNull)
            throw new Win32Exception();

        Application.Current.RegisterWindow(this);
        ApplyWindowTheme();

        var nativeSource = _xamlHost.As<IDesktopWindowXamlSourceNative2>();
        nativeSource.AttachToWindow(_hwnd);
        nativeSource.GetWindowHandle(out _xamlHwnd);
        InitializeContentRoot();

        PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_LAYERED | WINDOW_EX_STYLE.WS_EX_NOPARENTNOTIFY |
                       WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_NOACTIVATE,
            lpClassName: TitleBarClassName,
            lpWindowName: string.Empty,
            dwStyle: WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_MINIMIZEBOX | WINDOW_STYLE.WS_MAXIMIZEBOX,
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

        if (WindowsVersion.IsWin11OrNewer)
        {
            _maximizeButtonHwnd = PInvoke.CreateWindowEx(
                dwExStyle: WINDOW_EX_STYLE.WS_EX_NOPARENTNOTIFY,
                lpClassName: "BUTTON",
                lpWindowName: string.Empty,
                dwStyle: WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_DISABLED | (WINDOW_STYLE)0x0000000b,
                X: 0,
                Y: 0,
                nWidth: 0,
                nHeight: 0,
                hWndParent: _titleBarHwnd,
                hMenu: default,
                hInstance: s_hModule,
                lpParam: null);
        }

        UpdateDpi(PInvoke.GetDpiForWindow(_hwnd));
        EnableResizeLayoutSynchronization(_hwnd, true);
    }

    private void InitializeContentRoot()
    {
        _defaultCaptionButtons.Window = this;
        ApplyXamlRootRequestedTheme();

        Canvas.SetZIndex(_contentPresenter, 0);
        Canvas.SetZIndex(_defaultCaptionButtons, 1);

        _contentRoot.Children.Add(_contentPresenter);
        _contentRoot.Children.Add(_defaultCaptionButtons);
        _xamlHost.Content = _contentRoot;
    }

    private void ApplyXamlRootRequestedTheme()
    {
        _contentRoot.RequestedTheme = Application.Current.RequestedTheme == Windows.UI.Xaml.ApplicationTheme.Dark
            ? Windows.UI.Xaml.ElementTheme.Dark
            : Windows.UI.Xaml.ElementTheme.Light;
    }

    private IFrameworkApplicationPrivate FrameworkAppPrivate { get; } = Windows.UI.Xaml.Application.Current.As<IFrameworkApplicationPrivate>();

    private LRESULT WndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_ACTIVATE:
                var isActive = (wParam.Value & 0xffff) != 0;
                OnActivated(isActive);
                _captionButtons?.IsWindowActive(isActive);
                if (isActive)
                    Application.Current.OnWindowActivated(this);
                return default;

            case PInvoke.WM_GETMINMAXINFO:
                HandleGetMinMaxInfo(lParam);
                return default;

            case PInvoke.WM_DPICHANGED:
                UpdateDpi((uint)(wParam.Value >> 16));
                var suggestedRect = (RECT*)lParam.Value;
                PInvoke.SetWindowPos(_hwnd, default, suggestedRect->left, suggestedRect->top, suggestedRect->Width, suggestedRect->Height,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
                return default;

            case PInvoke.WM_NCCALCSIZE:
                if (ExtendsContentIntoTitleBar && TryHandleNcCalcSize(wParam, lParam, out var ncCalcResult))
                    return ncCalcResult;
                UpdateFrameMargins();
                break;

            case PInvoke.WM_PAINT:
                if (TryHandlePaint())
                    return default;
                break;

            case PInvoke.WM_NCHITTEST:
                if (ExtendsContentIntoTitleBar && TryHitTestTitleBar(lParam, out var hitTestResult))
                    return hitTestResult;
                break;

            case PInvoke.WM_NCRBUTTONUP:
                if (ExtendsContentIntoTitleBar && wParam.Value == HTCAPTION && TryShowSystemMenu(lParam))
                    return default;
                break;

            case PInvoke.WM_GETTITLEBARINFOEX:
                if (ExtendsContentIntoTitleBar && TryHandleGetTitleBarInfo(lParam))
                    return new LRESULT(1);
                break;

            case PInvoke.WM_SIZE:
                _isMaximized = PInvoke.IsZoomed(_hwnd);
                if (wParam.Value != PInvoke.SIZE_MINIMIZED)
                {
                    PInvoke.GetClientRect(_hwnd, out RECT cr);
                    OnSizeChanged(cr.Width, cr.Height);
                    UpdateXamlIslandBounds(cr);
                    UpdateTitleBarWindow();

                    PInvoke.SendMessage(Application.CoreHwnd, PInvoke.WM_SIZE, wParam, lParam);

                    FrameworkAppPrivate.SetSynchronizationWindow(_hwnd);
                    RepositionXamlPopups();
                }
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
