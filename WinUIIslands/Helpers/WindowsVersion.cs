using Windows.Win32.System.SystemInformation;

namespace WinUIIslands;

internal static class WindowsVersion
{
    private static readonly uint s_build = QueryBuildNumber();

    public static bool IsWin11OrNewer => s_build >= 22000;

    public static bool Is22H2OrNewer => s_build >= 22621;

    private static unsafe uint QueryBuildNumber()
    {
        OSVERSIONINFOW version = new()
        {
            dwOSVersionInfoSize = (uint)sizeof(OSVERSIONINFOW),
        };

        return Windows.Wdk.PInvoke.RtlGetVersion(ref version) >= 0 ? version.dwBuildNumber : 0;
    }
}
