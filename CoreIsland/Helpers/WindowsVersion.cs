using System.Runtime.InteropServices;

namespace CoreIsland;

internal static partial class WindowsVersion
{
    private static readonly uint s_build = QueryBuildNumber();

    public static bool IsWin11OrNewer => s_build >= 22000;

    public static bool Is22H2OrNewer => s_build >= 22621;

    private static unsafe uint QueryBuildNumber()
    {
        RTL_OSVERSIONINFOW version = new()
        {
            dwOSVersionInfoSize = (uint)sizeof(RTL_OSVERSIONINFOW),
        };

        return RtlGetVersion(ref version) >= 0 ? version.dwBuildNumber : 0;
    }

    [LibraryImport("ntdll.dll")]
    private static partial int RtlGetVersion(ref RTL_OSVERSIONINFOW version);

    private unsafe struct RTL_OSVERSIONINFOW
    {
        public uint dwOSVersionInfoSize;
        public uint dwMajorVersion;
        public uint dwMinorVersion;
        public uint dwBuildNumber;
        public uint dwPlatformId;
        public fixed char szCSDVersion[128];
    }
}
