using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public unsafe partial class Window
{
    private static readonly ConcurrentDictionary<HWND, WeakReference<Window>> s_parentToChildMapping = new();
    private static readonly WINEVENTPROC s_winEventProc = (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc;

    private readonly HWND _hwndParent;
    private UnhookWinEventSafeHandle? _winEventHook;

    public void ActivateAsChild()
    {
        if (_hwndParent.IsNull)
            throw new InvalidOperationException("Parent window handle is not set.");
        
        Loading?.Invoke(this, EventArgs.Empty);
        s_parentToChildMapping[_hwndParent] = new WeakReference<Window>(this);
        var threadId = PInvoke.GetWindowThreadProcessId(_hwndParent, out var processId);
        _winEventHook = PInvoke.SetWinEventHook(
            PInvoke.EVENT_OBJECT_LOCATIONCHANGE, PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
            null, s_winEventProc, processId, threadId, PInvoke.WINEVENT_OUTOFCONTEXT);

        if (_winEventHook.IsInvalid)
            throw new Win32Exception();

        // this is important
        PInvoke.GetClientRect(_hwndParent, out var rc);
        PInvoke.SetWindowPos(_hwnd, default, 0, 0, rc.Width, rc.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER);

        PInvoke.ShowWindow(_hwnd, SHOW_WINDOW_CMD.SW_SHOWNORMAL);
        PInvoke.UpdateWindow(_hwnd);
        Loaded?.Invoke(this, default);
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
        PInvoke.SetWindowPos(windowInstance._hwnd, default, default, default, rc.Width, rc.Height, SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE);
    }
}
