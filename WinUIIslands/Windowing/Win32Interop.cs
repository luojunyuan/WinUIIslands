namespace WinUIIslands.Windowing;

public static class Win32Interop
{
    public static WindowId GetWindowIdFromWindow(nint hwnd) => new((uint)hwnd);
}
