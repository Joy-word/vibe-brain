using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BrainEx.Models;

public enum TaskStatus
{
    Pending,     // 待处理（队列中）
    InProgress,  // 进行中
    Done         // 已完成
}

public class TaskItem : INotifyPropertyChanged
{
    private TaskStatus _status = TaskStatus.Pending;
    private string _summary = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>Windows UserNotification Id，用于事后清除通知</summary>
    public uint SystemNotifId { get; set; }

    /// <summary>
    /// 通知到达时捕获的目标窗口句柄。
    /// 多开场景下用于精确激活发出通知的那个编辑器窗口。
    /// </summary>
    public nint TargetHwnd { get; set; }

    /// <summary>
    /// 通知到达时捕获的窗口标题，如 "LoginPage.cs — my-project — Cursor"。
    /// 用于在 UI 上展示是哪个项目窗口发出的通知。
    /// </summary>
    public string WindowTitle { get; set; } = string.Empty;

    /// <summary>
    /// 从窗口标题中提取工作区/项目名。
    /// Cursor/VSCode 标题格式：文件名 — 工作区 — 应用名
    /// 取倒数第二段；若格式不符则返回空字符串。
    /// </summary>
    public string WorkspaceLabel
    {
        get
        {
            if (string.IsNullOrEmpty(WindowTitle)) return string.Empty;
            // 兼容中文破折号（—）和 ASCII dash ( - )
            var parts = WindowTitle.Split(['—', '–', '-'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            // 至少要有 3 段（文件 / 工作区 / 应用名），取倒数第二段
            return parts.Length >= 3 ? parts[^2] : string.Empty;
        }
    }

    /// <summary>来源应用名称，如 Cursor、VSCode</summary>
    public string SourceApp { get; set; } = string.Empty;

    /// <summary>通知标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>通知正文或 AI 回复摘要</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>用户自己写的下一步备注</summary>
    public string Summary
    {
        get => _summary;
        set { _summary = value; OnPropertyChanged(); }
    }

    public TaskStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPending)); OnPropertyChanged(nameof(IsInProgress)); OnPropertyChanged(nameof(IsDone)); OnPropertyChanged(nameof(IsNotDone)); }
    }

    public bool IsPending    => Status == TaskStatus.Pending;
    public bool IsInProgress => Status == TaskStatus.InProgress;
    public bool IsDone       => Status == TaskStatus.Done;
    public bool IsNotDone    => Status != TaskStatus.Done;

    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>显示用时间戳，如 "10:23"</summary>
    public string TimeLabel => CreatedAt.ToString("HH:mm");

    /// <summary>来源 App 首字母，用于头像占位</summary>
    public string AppInitial => string.IsNullOrEmpty(SourceApp) ? "?" : SourceApp[0].ToString().ToUpper();

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
