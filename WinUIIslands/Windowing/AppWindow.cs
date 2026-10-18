using Windows.Graphics;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinUIIslands.Windowing;

public sealed class AppWindowClosingEventArgs : EventArgs
{
    public bool Cancel { get; set; }
}

public sealed unsafe partial class AppWindow
{
    private static readonly Dictionary<nint, AppWindow> s_registry = [];

    private readonly OverlappedPresenter _presenter;
    private nint _largeIcon;
    private nint _smallIcon;

    internal HWND Hwnd { get; }

    public WindowId Id => new(unchecked((ulong)Hwnd.Value));

    public WindowId OwnerWindowId => new(unchecked((ulong)GetWindow((nint)Hwnd.Value, GW_OWNER)));

    public event EventHandler<AppWindowClosingEventArgs>? Closing;

    public AppWindowPresenter Presenter => _presenter;

    public PointInt32 Position
    {
        get
        {
            if (!PInvoke.GetWindowRect(Hwnd, out var rect))
                throw new Win32Exception();

            return new PointInt32(rect.left, rect.top);
        }
        set => Move(value);
    }

    public SizeInt32 Size
    {
        get
        {
            if (!PInvoke.GetWindowRect(Hwnd, out var rect))
                throw new Win32Exception();

            return new SizeInt32(rect.Width, rect.Height);
        }
        set => Resize(value);
    }

    public SizeInt32 ClientSize
    {
        get
        {
            if (!PInvoke.GetClientRect(Hwnd, out var rect))
                throw new Win32Exception();

            return new SizeInt32(rect.Width, rect.Height);
        }
    }

    public string Title
    {
        get => GetWindowTitle(Hwnd);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!PInvoke.SetWindowText(Hwnd, value))
                throw new Win32Exception();
        }
    }

    public bool IsShownInSwitchers
    {
        get
        {
            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            return (exStyle & WINDOW_EX_STYLE.WS_EX_TOOLWINDOW) == 0;
        }
        set
        {
            var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            exStyle = value
                ? exStyle & ~WINDOW_EX_STYLE.WS_EX_TOOLWINDOW
                : exStyle | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW;

            PInvoke.SetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)exStyle);
            PInvoke.SetWindowPos(Hwnd, default, 0, 0, 0, 0,
                SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE |
                SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE |
                SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
        }
    }

    public bool IsVisible => IsWindowVisible(Hwnd) != 0;

    private AppWindow(HWND hwnd)
    {
        Hwnd = hwnd;
        _presenter = new OverlappedPresenter(this);
    }

    public static AppWindow GetFromWindowId(WindowId windowId)
    {
        var key = unchecked((nint)windowId.Value);
        if (s_registry.TryGetValue(key, out var existing))
            return existing;

        var appWindow = new AppWindow(new HWND(key));
        s_registry[key] = appWindow;
        return appWindow;
    }

    public void Move(PointInt32 position)
    {
        if (!PInvoke.SetWindowPos(Hwnd, default, position.X, position.Y, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void MoveAndResize(RectInt32 rect)
    {
        if (rect.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(rect), "Width must be positive.");
        if (rect.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(rect), "Height must be positive.");

        if (!PInvoke.SetWindowPos(Hwnd, default, rect.X, rect.Y, rect.Width, rect.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void ResizeClient(SizeInt32 size)
    {
        if (size.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Width must be positive.");
        if (size.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Height must be positive.");

        uint dpi = PInvoke.GetDpiForWindow(Hwnd);
        var style = (WINDOW_STYLE)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE);
        var exStyle = (WINDOW_EX_STYLE)PInvoke.GetWindowLongAnyCPU(Hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        RECT rect = new() { right = size.Width, bottom = size.Height };
        if (!PInvoke.AdjustWindowRectExForDpi(ref rect, style, false, exStyle, dpi))
            throw new Win32Exception();

        if (!PInvoke.SetWindowPos(Hwnd, default, 0, 0, rect.Width, rect.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void Resize(SizeInt32 size)
    {
        if (size.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Width must be positive.");
        if (size.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Height must be positive.");

        if (!PInvoke.SetWindowPos(Hwnd, default, 0, 0, size.Width, size.Height,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void MoveInZOrderAtTop()
    {
        if (!PInvoke.SetWindowPos(Hwnd, default, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void MoveInZOrderAtBottom()
    {
        if (!PInvoke.SetWindowPos(Hwnd, new HWND((nint)1), 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void MoveInZOrder(AppWindow insertAfterWindow)
    {
        ArgumentNullException.ThrowIfNull(insertAfterWindow);
        if (!PInvoke.SetWindowPos(Hwnd, insertAfterWindow.Hwnd, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void MoveInZOrderBelow(WindowId windowId)
    {
        var insertAfter = new HWND(unchecked((nint)windowId.Value));
        if (!PInvoke.SetWindowPos(Hwnd, insertAfter, 0, 0, 0, 0,
            SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE))
            throw new Win32Exception();
    }

    public void Close() => PInvoke.SendMessage(Hwnd, PInvoke.WM_CLOSE, default, default);

    public void Destroy()
    {
        if (!PInvoke.DestroyWindow(Hwnd))
            throw new Win32Exception();
    }

    public void Show() => Show(true);

    public void Show(bool activate) => PInvoke.ShowWindow(Hwnd,
        activate ? SHOW_WINDOW_CMD.SW_SHOW : SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);

    public void Hide() => PInvoke.ShowWindow(Hwnd, SHOW_WINDOW_CMD.SW_HIDE);

    public void SetIcon(string iconPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);

        nint largeIcon = LoadWindowIcon(iconPath, SM_CXICON, SM_CYICON);
        nint smallIcon;
        try
        {
            smallIcon = LoadWindowIcon(iconPath, SM_CXSMICON, SM_CYSMICON);
        }
        catch
        {
            _ = DestroyIcon(largeIcon);
            throw;
        }

        PInvoke.SendMessage(Hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_SMALL), new LPARAM(smallIcon));
        PInvoke.SendMessage(Hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_BIG), new LPARAM(largeIcon));

        ReplaceOwnedIcon(ref _smallIcon, smallIcon);
        ReplaceOwnedIcon(ref _largeIcon, largeIcon);
    }

    public void SetTaskbarIcon(string iconPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        nint icon = LoadWindowIcon(iconPath, SM_CXICON, SM_CYICON);
        PInvoke.SendMessage(Hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_BIG), new LPARAM(icon));
        ReplaceOwnedIcon(ref _largeIcon, icon);
    }

    public void SetTitleBarIcon(string iconPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);
        nint icon = LoadWindowIcon(iconPath, SM_CXSMICON, SM_CYSMICON);
        PInvoke.SendMessage(Hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_SMALL), new LPARAM(icon));
        ReplaceOwnedIcon(ref _smallIcon, icon);
    }

    internal static string GetWindowTitle(HWND hwnd)
    {
        int length = GetWindowTextLengthW(hwnd);
        if (length == 0)
            return string.Empty;

        char[] buffer = new char[length + 1];
        fixed (char* text = buffer)
        {
            int copied = GetWindowTextW(hwnd, text, buffer.Length);
            return new string(buffer, 0, copied);
        }
    }

    internal static void OnWindowDestroyed(HWND hwnd)
    {
        if (s_registry.Remove((nint)hwnd.Value, out var appWindow))
        {
            PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_SMALL), default);
            PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, new WPARAM((nuint)ICON_BIG), default);
            appWindow.DestroyOwnedIcons();
        }
    }

    private void DestroyOwnedIcons()
    {
        if (_smallIcon != 0)
        {
            _ = DestroyIcon(_smallIcon);
            _smallIcon = 0;
        }

        if (_largeIcon != 0)
        {
            _ = DestroyIcon(_largeIcon);
            _largeIcon = 0;
        }
    }

    internal static bool OnWindowClosing(HWND hwnd) =>
        s_registry.TryGetValue((nint)hwnd.Value, out var appWindow) && appWindow.RaiseClosing();

    private bool RaiseClosing()
    {
        var args = new AppWindowClosingEventArgs();
        Closing?.Invoke(this, args);
        return args.Cancel;
    }

    private static nint LoadWindowIcon(string iconPath, int widthMetric, int heightMetric)
    {
        int width = Math.Max(1, GetSystemMetrics(widthMetric));
        int height = Math.Max(1, GetSystemMetrics(heightMetric));
        nint icon = LoadImageW(default, iconPath, IMAGE_ICON, width, height, LR_LOADFROMFILE);
        if (icon == 0)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        return icon;
    }

    private static void ReplaceOwnedIcon(ref nint current, nint replacement)
    {
        if (current != 0)
            _ = DestroyIcon(current);

        current = replacement;
    }

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x0010;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int SM_CXICON = 11;
    private const int SM_CYICON = 12;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;
    private const uint GW_OWNER = 4;

    [LibraryImport("user32.dll", EntryPoint = "LoadImageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint LoadImageW(nint hInstance, string name, uint type, int cx, int cy, uint fuLoad);

    [LibraryImport("user32.dll", EntryPoint = "DestroyIcon")]
    private static partial int DestroyIcon(nint hIcon);

    [LibraryImport("user32.dll", EntryPoint = "GetSystemMetrics")]
    private static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    private static partial int GetWindowTextLengthW(HWND hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
    private static partial int GetWindowTextW(HWND hWnd, char* lpString, int nMaxCount);

    [LibraryImport("user32.dll", EntryPoint = "IsWindowVisible")]
    private static partial int IsWindowVisible(HWND hWnd);

    [LibraryImport("user32.dll", EntryPoint = "GetWindow")]
    private static partial nint GetWindow(nint hWnd, uint uCmd);
}
