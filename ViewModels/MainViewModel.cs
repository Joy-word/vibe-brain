using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BrainEx.Models;
using BrainEx.Services;
using BrainEx.Resources.Strings;

namespace BrainEx.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly NotificationListenerService _notifService;
    private CancellationTokenSource? _cts;

    public ObservableCollection<TaskItem> Queue { get; } = new();

    // ── 显示已完成切换 ──
    private bool _showDone = false;
    public bool ShowDone
    {
        get => _showDone;
        set
        {
            _showDone = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowDoneText));
            OnPropertyChanged(nameof(VisibleQueue));
        }
    }
    public string ShowDoneText => ShowDone ? AppResources.BtnHideDone : AppResources.BtnShowDone;

    /// <summary>UI 绑定的实际列表（按 ShowDone 过滤）</summary>
    public IEnumerable<TaskItem> VisibleQueue =>
        ShowDone ? Queue : Queue.Where(t => !t.IsDone);

    public ICommand ToggleShowDoneCommand { get; private set; } = null!;
    // ── 监听状态 ──
    private bool _isListening;
    public bool IsListening
    {
        get => _isListening;
        set { _isListening = value; OnPropertyChanged(); OnPropertyChanged(nameof(ListenButtonText)); }
    }

    private string _statusText = AppResources.StatusIdle;
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public string ListenButtonText => IsListening ? AppResources.BtnStop : AppResources.BtnListen;

    // ── 置顶状态 ──
    private bool _isPinned;
    public bool IsPinned
    {
        get => _isPinned;
        set
        {
            _isPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PinButtonText));
            OnPropertyChanged(nameof(PinButtonColor));
            WindowPinner.SetTopmost(value);
        }
    }
    public string PinButtonText  => IsPinned ? "📌" : "📍";
    public string PinButtonColor => IsPinned ? "#FF9500" : "#8E8E93";

    // ── 统计 ──
    public int PendingCount    => Queue.Count(t => t.IsPending);
    public int InProgressCount => Queue.Count(t => t.IsInProgress);

    // ── Commands ──
    public ICommand ToggleListenCommand { get; }
    public ICommand TogglePinCommand    { get; }
    public ICommand StartTaskCommand    { get; }
    public ICommand MarkDoneCommand     { get; }
    public ICommand ClearDoneCommand    { get; }
    public ICommand AddManualCommand    { get; }
    public ICommand FocusIdeCommand     { get; }

    public MainViewModel()
    {
        _notifService = new NotificationListenerService();
        _notifService.TaskArrived += OnTaskArrived;

        // 队列变化时同步刷新 VisibleQueue
        Queue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(VisibleQueue));

        ToggleShowDoneCommand = new Command(() => ShowDone = !ShowDone);

        ToggleListenCommand = new Command(async () => await ToggleListenAsync());

        TogglePinCommand = new Command(() => IsPinned = !IsPinned);

        // 开始：激活对应 IDE 窗口，标记进行中，清除系统通知
        StartTaskCommand = new Command<TaskItem>(item =>
        {
            if (item == null) return;
            foreach (var t in Queue.Where(t => t.IsInProgress))
                t.Status = Models.TaskStatus.Pending;
            item.Status = Models.TaskStatus.InProgress;
            RefreshCounts();
            // 精确激活：优先 WindowTitle，其次 TargetHwnd，最后进程名
            AppActivator.Activate(item.TargetHwnd, item.SourceApp, item.WindowTitle);
            // 清除 Windows 通知中心里这条通知
            _notifService.DismissNotification(item.SystemNotifId);
        });

        // 完成：标记 Done，刷新可见队列
        MarkDoneCommand = new Command<TaskItem>(item =>
        {
            if (item == null) return;
            item.Status = Models.TaskStatus.Done;
            RefreshCounts();
            OnPropertyChanged(nameof(VisibleQueue));
        });

        // 单独的"聚焦 IDE"命令（不改状态，只跳窗口）
        FocusIdeCommand = new Command<TaskItem>(item =>
        {
            if (item == null) return;
            AppActivator.Activate(item.TargetHwnd, item.SourceApp, item.WindowTitle);
        });

        ClearDoneCommand = new Command(() =>
        {
            var done = Queue.Where(t => t.Status == Models.TaskStatus.Done).ToList();
            foreach (var t in done) Queue.Remove(t);
            RefreshCounts();
            OnPropertyChanged(nameof(VisibleQueue));
        });

        AddManualCommand = new Command<string>(title =>
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            Queue.Add(new TaskItem { SourceApp = AppResources.SourceManual, Title = title });
            RefreshCounts();
        });
    }

    private async Task ToggleListenAsync()
    {
        if (IsListening)
        {
            _cts?.Cancel();
            _notifService.Stop();
            IsListening = false;
            StatusText = AppResources.StatusStopped;
        }
        else
        {
            _cts = new CancellationTokenSource();
            try
            {
                bool granted = await _notifService.RequestPermissionAsync();
                if (!granted)
                {
                    StatusText = AppResources.StatusNoPermission;
                    return;
                }
                await _notifService.StartAsync(_cts.Token);
                IsListening = true;
                StatusText = AppResources.StatusListening;
            }
            catch (Exception ex)
            {
                StatusText = string.Format(AppResources.StatusError, ex.Message);
            }
        }
    }

    private void OnTaskArrived(TaskItem task)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Queue.Add(task);   // 按时间顺序追加到末尾
            RefreshCounts();
        });
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(InProgressCount));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
