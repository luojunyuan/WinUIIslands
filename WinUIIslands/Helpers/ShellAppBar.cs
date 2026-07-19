using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace WinUIIslands;

internal static partial class ShellAppBar
{
    private const uint ABM_GETSTATE = 0x00000004;
    private const uint ABM_GETAUTOHIDEBAREX = 0x0000000b;
    private const uint ABS_AUTOHIDE = 0x00000001;

    private const uint ABE_LEFT = 0;
    private const uint ABE_TOP = 1;
    private const uint ABE_RIGHT = 2;
    private const uint ABE_BOTTOM = 3;

    public static unsafe bool HasAutoHideTaskbar(RECT monitorRect, uint edge)
    {
        APPBARDATA data = new()
        {
            cbSize = (uint)sizeof(APPBARDATA),
            uEdge = edge,
            rc = monitorRect,
        };

        return SHAppBarMessage(ABM_GETAUTOHIDEBAREX, ref data) != 0;
    }

    public static bool HasAnyAutoHideTaskbar()
    {
        APPBARDATA data = CreateAppBarData();
        return (SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;
    }

    public static bool HasTopAutoHideTaskbar(RECT monitorRect) => HasAutoHideTaskbar(monitorRect, ABE_TOP);

    public static bool HasBottomAutoHideTaskbar(RECT monitorRect) => HasAutoHideTaskbar(monitorRect, ABE_BOTTOM);

    public static bool HasLeftAutoHideTaskbar(RECT monitorRect) => HasAutoHideTaskbar(monitorRect, ABE_LEFT);

    public static bool HasRightAutoHideTaskbar(RECT monitorRect) => HasAutoHideTaskbar(monitorRect, ABE_RIGHT);

    private static unsafe APPBARDATA CreateAppBarData() => new()
    {
        cbSize = (uint)sizeof(APPBARDATA),
    };

    [LibraryImport("shell32.dll")]
    private static partial nuint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public IntPtr lParam;
    }
}
