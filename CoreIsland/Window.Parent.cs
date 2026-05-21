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
    private sealed class ParentEntry
    {
        public UnhookWinEventSafeHandle? Hook;
        public readonly List<WeakReference<Window>> Children = [];
    }

    private static readonly ConcurrentDictionary<HWND, ParentEntry> s_parentToChildMapping = new();
    private static readonly WINEVENTPROC s_winEventProc = (delegate* unmanaged[Stdcall]<HWINEVENTHOOK, uint, HWND, int, int, uint, uint, void>)&WinEventProc;

    private readonly HWND _hwndParent;

    /// <summary>
    /// Activates the current window as a child layout element.
    /// </summary>
    public void ActivateAsChild()
    {
        if (_hwndParent.IsNull)
            throw new InvalidOperationException("Parent window handle is not set.");

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

        PInvoke.GetClientRect(_hwndParent, out var rc);
        PInvoke.SetWindowPos(_hwnd, HWND.HWND_TOP, 0, 0, rc.Width, rc.Height, default);

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
