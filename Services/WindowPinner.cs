namespace BrainEx.Services;

/// <summary>
/// 控制主窗口"始终置顶"（Always on Top）。
/// </summary>
public static class WindowPinner
{
#if WINDOWS
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        nint hWnd, nint hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    private static readonly nint HWND_TOPMOST    = new(-1);
    private static readonly nint HWND_NOTOPMOST  = new(-2);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    private static nint GetMainWindowHandle()
    {
        // MAUI on Windows: 当前 App 的主窗口
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUi)
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(winUi);
            return hwnd;
        }
        return nint.Zero;
    }

    public static void SetTopmost(bool topmost)
    {
        nint hwnd = GetMainWindowHandle();
        if (hwnd == nint.Zero) return;
        SetWindowPos(hwnd,
            topmost ? HWND_TOPMOST : HWND_NOTOPMOST,
            0, 0, 0, 0,
            SWP_NOMOVE | SWP_NOSIZE);
    }
#else
    public static void SetTopmost(bool topmost) { /* 非 Windows 平台无操作 */ }
#endif
}
