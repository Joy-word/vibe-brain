using Microsoft.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace BrainEx.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        SetInitialWindowSize();
    }

    private static void SetInitialWindowSize()
    {
        // 获取 MAUI 主窗口句柄
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window winUi) return;

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(winUi);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        // 初始尺寸：宽 380，高 680（紧凑侧边栏风格）
        appWindow.Resize(new SizeInt32(720, 1080));

        //// 移动到屏幕右上角（可选，像通知中心一样的位置）
        //var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
        //int screenW = displayArea.WorkArea.Width;
        //int x = screenW - 380 - 16;   // 右边距 16px
        //int y = 40;                     // 顶部留 40px
        //appWindow.Move(new PointInt32(x, y));
    }
}
