using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public unsafe partial class NativeWindow
{
    private const string ClassName = "CoreIsland_Native_Wnd";
    private static readonly FreeLibrarySafeHandle s_hModule = PInvoke.GetModuleHandle();
    private static readonly WNDPROC s_wndProc = (delegate* unmanaged[Stdcall]<HWND, uint, WPARAM, LPARAM, LRESULT>)&StaticWndProc;

    static NativeWindow()
    {
        fixed (char* pClassName = ClassName)
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

            if (GCHandle.FromIntPtr(pSelf).Target is NativeWindow self)
                self._hwnd = hwnd;
        }
        else
        {
            var userData = PInvoke.GetWindowLongAnyCPU(hwnd, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA);
            if (userData != 0 && GCHandle.FromIntPtr(userData).Target is NativeWindow self)
                return self.WndProc(msg, wParam, lParam);
        }

        return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
    }
}

public unsafe partial class NativeWindow
{
    private readonly GCHandle _selfHandle;
    private HWND _hwnd;

    private static readonly ConcurrentDictionary<HWND, WeakReference<NativeWindow>> s_parentToChildMapping = new();
    private static readonly WINEVENTPROC s_winEventProc = (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc;

    private readonly HWND _hwndParent;
    private UnhookWinEventSafeHandle? _winEventHook;

    public NativeWindow(nint hParent = default)
    {
        _hwndParent = new HWND(hParent);
        _selfHandle = GCHandle.Alloc(this);
        var hwnd = PInvoke.CreateWindowEx(
            dwExStyle: WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME,
            lpClassName: ClassName,
            lpWindowName: string.Empty,
            dwStyle: _hwndParent.IsNull ? WINDOW_STYLE.WS_OVERLAPPEDWINDOW : WINDOW_STYLE.WS_CHILDWINDOW,
            X: PInvoke.CW_USEDEFAULT,
            Y: PInvoke.CW_USEDEFAULT,
            nWidth: PInvoke.CW_USEDEFAULT,
            nHeight: PInvoke.CW_USEDEFAULT,
            hWndParent: _hwndParent,
            hMenu: default,
            hInstance: s_hModule,
            lpParam: (void*)GCHandle.ToIntPtr(_selfHandle));

        if (hwnd.IsNull)
            throw new Win32Exception();
    }

    public void ActivateAsChild()
    {
        if (_hwndParent.IsNull)
            throw new InvalidOperationException("Parent window handle is not set.");

        var threadId = PInvoke.GetWindowThreadProcessId(_hwndParent, out var processId);
        _winEventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE, PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            null, s_winEventProc, processId, threadId, PInvoke.WINEVENT_OUTOFCONTEXT);

        if (_winEventHook.IsInvalid)
            throw new Win32Exception();

        PInvoke.GetClientRect(_hwndParent, out var rc);
        PInvoke.SetWindowPos(_hwnd, HWND.HWND_TOP, 0, 0, rc.Width, rc.Height, default);

        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(_hwnd);
    }

    public void Close() => PInvoke.SendMessage(_hwnd, PInvoke.WM_CLOSE, 0, 0);

    public event EventHandler<WindowSizeChangedEventArgs>? WindowSizeChanged;

    private void OnSizeChanged(int width, int height)
    {
        WindowSizeChanged?.Invoke(this, new WindowSizeChangedEventArgs(width, height));
    }

    private LRESULT WndProc(uint msg, WPARAM wParam, LPARAM lParam)
    {
        switch (msg)
        {
            case PInvoke.WM_DESTROY:
                _winEventHook?.Close();
                s_parentToChildMapping.TryRemove(_hwndParent, out _);
                if (_selfHandle.IsAllocated)
                    _selfHandle.Free();
                return default;
        }

        return PInvoke.DefWindowProc(_hwnd, msg, wParam, lParam);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (idObject != (int)OBJECT_IDENTIFIER.OBJID_WINDOW || idChild != PInvoke.CHILDID_SELF)
            return;

        if (eventType != PInvoke.EVENT_OBJECT_LOCATIONCHANGE)
            return;

        if (!s_parentToChildMapping.TryGetValue(hwnd, out var weakRef) || !weakRef.TryGetTarget(out var windowInstance))
            return;

        PInvoke.GetClientRect(hwnd, out var rc);
        PInvoke.SetWindowPos(windowInstance._hwnd, default, default, default, rc.Width, rc.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
        windowInstance.OnSizeChanged(rc.Width, rc.Height);
    }
}
