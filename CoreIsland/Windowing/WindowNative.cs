namespace CoreIsland.Windowing;

public static class WindowNative
{
    public static nint GetWindowHandle(Window window) => window.Hwnd;
}
