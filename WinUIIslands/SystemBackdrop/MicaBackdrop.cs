using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace WinUIIslands;

public class MicaBackdrop : SystemBackdrop
{
    public MicaKind Kind { get; set; } = MicaKind.Base;

    protected override void OnApply(Window window)
    {
        SetBackdrop(window.Hwnd, Kind switch
        {
            MicaKind.BaseAlt => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_TABBEDWINDOW,
            _ => DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW,
        });
    }

    protected override void OnRemove(Window window)
    {
        SetBackdrop(window.Hwnd, DWM_SYSTEMBACKDROP_TYPE.DWMSBT_NONE);
    }

    private static void SetBackdrop(HWND hwnd, DWM_SYSTEMBACKDROP_TYPE type)
    {
        var enumSpan = MemoryMarshal.CreateSpan(ref type, 1);
        _ = PInvoke.DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE, MemoryMarshal.AsBytes(enumSpan));
    }
}

public enum MicaKind
{
    Base,
    BaseAlt,
}
