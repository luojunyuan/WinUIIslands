using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Windows.Win32;

internal static partial class PInvoke // SHAppBarMessage
{
    [LibraryImport("shell32.dll")]
    public static partial nuint SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);
}

// APPBARDATA is architecture-specific in Win32 metadata, so CsWin32 cannot generate it for this AnyCPU library.
[StructLayout(LayoutKind.Sequential)]
internal struct APPBARDATA
{
    public uint cbSize;
    public HWND hWnd;
    public uint uCallbackMessage;
    public uint uEdge;
    public RECT rc;
    public LPARAM lParam;
}
