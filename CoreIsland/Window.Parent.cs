using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CoreIsland.Windowing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using Windows.Win32.UI.WindowsAndMessaging;

namespace CoreIsland;

public unsafe partial class Window
{
    private sealed class ParentEntry
    {
        public UnhookWinEventSafeHandle? Hook;
        public readonly List<WeakReference<Window>> Children = [];
    }

    private static readonly ConcurrentDictionary<HWND, ParentEntry> s_parentToChildMapping = new();
    private static readonly WINEVENTPROC s_winEventProc = (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc;

    private HWND _hwndParent;

    internal bool HasParentWindoow => !_hwndParent.IsNull;

    /// <summary>
    /// Activates the current window as a child layout element.
    /// </summary>
    public void ActivateAsChild(nint hParent)
    {
        _hwndParent = new(hParent);
        Loading?.Invoke(this, EventArgs.Empty);

        var entry = s_parentToChildMapping.GetOrAdd(_hwndParent, _ => new ParentEntry());
        entry.Children.Add(new WeakReference<Window>(this));
        if (entry.Hook is null)
        {
            var threadId = PInvoke.GetWindowThreadProcessId(_hwndParent, out var processId);
            entry.Hook = PInvoke.SetWinEventHook(
                PInvoke.EVENT_OBJECT_LOCATIONCHANGE, PInvoke.EVENT_OBJECT_LOCATIONCHANGE,
                null, s_winEventProc, processId, threadId, PInvoke.WINEVENT_OUTOFCONTEXT);

            if (entry.Hook.IsInvalid)
                throw new Win32Exception();
        }

        var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        style &= ~WINDOW_STYLE.WS_OVERLAPPEDWINDOW;
        style |= WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_CLIPSIBLINGS;
        PInvoke.SetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, (nint)style);

        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle &= ~WINDOW_EX_STYLE.WS_EX_DLGMODALFRAME;
        PInvoke.SetWindowLongAnyCPU(_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);

        PInvoke.SetParent(_hwnd, _hwndParent);

        PInvoke.GetClientRect(_hwndParent, out var rc);
        PInvoke.SetWindowPos(_hwnd, HWND.HWND_TOP,
            0, 0, rc.Width, rc.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
        );

        Loaded?.Invoke(this, default);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static void WinEventProc(HWINEVENTHOOK hWinEventHook, uint eventType, HWND hwnd, int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (idObject != (int)OBJECT_IDENTIFIER.OBJID_WINDOW || idChild != PInvoke.CHILDID_SELF)
            return;

        if (eventType != PInvoke.EVENT_OBJECT_LOCATIONCHANGE)
            return;

        if (!s_parentToChildMapping.TryGetValue(hwnd, out var entry))
            return;

        PInvoke.GetClientRect(hwnd, out var rc);

        for (int i = entry.Children.Count - 1; i >= 0; i--)
        {
            if (entry.Children[i].TryGetTarget(out var windowInstance))
            {
                PInvoke.SetWindowPos(windowInstance._hwnd, default, default, default, rc.Width, rc.Height,
                    SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);
            }
            else
            {
                entry.Children.RemoveAt(i);
            }
        }

        if (entry.Children.Count == 0)
        {
            entry.Hook?.Close();
            s_parentToChildMapping.TryRemove(hwnd, out _);
        }
    }
}
