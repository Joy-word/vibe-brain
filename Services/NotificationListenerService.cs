using BrainEx.Models;

namespace BrainEx.Services;

/// <summary>
/// Windows 系统通知监听服务。
/// 使用 WinRT UserNotificationListener API（需要用户授权）。
/// </summary>
public class NotificationListenerService
{
    public event Action<TaskItem>? TaskArrived;

    private bool _isListening = false;

    // 只监听这些应用（可由用户配置）
    private readonly HashSet<string> _watchedApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "cursor",
        "code",          // VSCode
        "codex",
        "windsurf",
        "claude",
        "copilot"
    };

    public IReadOnlySet<string> WatchedApps => _watchedApps;

    public void AddWatchedApp(string appName) => _watchedApps.Add(appName);
    public void RemoveWatchedApp(string appName) => _watchedApps.Remove(appName);

#if WINDOWS
    private Windows.UI.Notifications.Management.UserNotificationListener? _listener;
    private readonly PeriodicTimer _pollTimer = new(TimeSpan.FromSeconds(2));

    public async Task<bool> RequestPermissionAsync()
    {
        _listener = Windows.UI.Notifications.Management.UserNotificationListener.Current;
        var status = await _listener.RequestAccessAsync();
        return status == Windows.UI.Notifications.Management.UserNotificationListenerAccessStatus.Allowed;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_isListening) return;
        if (_listener is null)
        {
            bool granted = await RequestPermissionAsync();
            if (!granted) throw new InvalidOperationException("未获得通知读取权限，请在 Windows 设置 → 通知 → 允许应用访问通知 中授权。");
        }

        _isListening = true;
        _ = PollLoopAsync(ct);
    }

    public void Stop() => _isListening = false;

    /// <summary>清除 Windows 通知中心里对应的通知条目</summary>
    public void DismissNotification(uint systemNotifId)
    {
        if (systemNotifId == 0 || _listener is null) return;
        try { _listener.RemoveNotification(systemNotifId); }
        catch { /* 通知已消失或无权限时静默 */ }
    }

    private uint _lastSeenId = 0;

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (_isListening && !ct.IsCancellationRequested)
        {
            try
            {
                await _pollTimer.WaitForNextTickAsync(ct);
                var notifications = await _listener!.GetNotificationsAsync(
                    Windows.UI.Notifications.NotificationKinds.Toast);

                foreach (var notif in notifications)
                {
                    if (notif.Id <= _lastSeenId) continue;
                    _lastSeenId = Math.Max(_lastSeenId, notif.Id);

                    var appInfo = notif.AppInfo;
                    string appName = appInfo?.DisplayInfo?.DisplayName ?? "Unknown";

                    bool isWatched = _watchedApps.Any(w =>
                        appName.Contains(w, StringComparison.OrdinalIgnoreCase));
                    if (!isWatched) continue;

                    var binding = notif.Notification?.Visual?.GetBinding(
                        Windows.UI.Notifications.KnownNotificationBindings.ToastGeneric);
                    if (binding is null) continue;

                    var elements = binding.GetTextElements();
                    string title = elements.ElementAtOrDefault(0)?.Text ?? appName;
                    string body  = elements.ElementAtOrDefault(1)?.Text ?? string.Empty;

                    // 通知到达瞬间，捕获最匹配的编辑器窗口句柄
                    var hwnd = AppActivator.CaptureTargetHwnd(appName, title, body);

                    var task = new TaskItem
                    {
                        SourceApp     = appName,
                        Title         = title,
                        Body          = body,
                        SystemNotifId = notif.Id,
                        TargetHwnd    = hwnd,
                        WindowTitle   = AppActivator.GetWindowTitle(hwnd),
                    };
                    TaskArrived?.Invoke(task);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* 静默跳过单次轮询错误 */ }
        }
    }
#else
    // 非 Windows 平台：模拟通知
    public Task<bool> RequestPermissionAsync() => Task.FromResult(true);

    public void DismissNotification(uint systemNotifId) { }

    public Task StartAsync(CancellationToken ct = default)
    {
        _isListening = true;
        _ = Task.Run(async () =>
        {
            while (_isListening && !ct.IsCancellationRequested)
            {
                await Task.Delay(8000, ct);
                TaskArrived?.Invoke(new TaskItem
                {
                    SourceApp = "Cursor",
                    Title     = "[模拟] 代码生成完成",
                    Body      = "LoginPage.cs 已生成，请确认逻辑是否正确。",
                });
            }
        }, ct);
        return Task.CompletedTask;
    }

    public void Stop() => _isListening = false;
#endif
}
