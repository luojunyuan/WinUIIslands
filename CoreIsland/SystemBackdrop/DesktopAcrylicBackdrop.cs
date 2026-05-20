using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace CoreIsland;

public class DesktopAcrylicBackdrop : SystemBackdrop
{
    internal override void Apply(Window window)
    {
        SetBackdrop(window.Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TRANSIENTWINDOW);
    }

    internal override void Remove(Window window)
    {
        SetBackdrop(window.Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
    }

    private static void SetBackdrop(HWND hwnd, DWM_SYSTEMBACKDROP_TYPE type)
    {
        var enumSpan = MemoryMarshal.CreateSpan(ref type, 1);
        _ = PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, MemoryMarshal.AsBytes(enumSpan));
    }
}
