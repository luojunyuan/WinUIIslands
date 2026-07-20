namespace WinUIIslands;

internal static class WindowsVersion
{
    public static bool IsWin11OrNewer => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);

    public static bool Is22H2OrNewer => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);
}
