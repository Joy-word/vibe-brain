using System.Runtime.InteropServices;
using System.Text;

namespace BrainEx.Services;

/// <summary>
/// 激活目标 IDE 窗口。
/// 优先使用通知到达时捕获的精确 HWND；
/// 回退到进程名匹配（适用于手动添加的任务）。
/// </summary>
public static class AppActivator
{
    // 各 IDE 的进程名（不含 .exe）
    private static readonly Dictionary<string, string[]> AppProcessMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["cursor"]   = ["cursor"],
            ["vscode"]   = ["code"],
            ["code"]     = ["code"],
            ["codex"]    = ["codex"],
            ["windsurf"] = ["windsurf"],
            ["claude"]   = ["claude"],
            ["copilot"]  = ["github copilot"],
        };

#if WINDOWS
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(nint hWnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(nint hWnd, int nCmdShow);
    [DllImport("user32.dll")] static extern bool IsIconic(nint hWnd);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(nint hWnd);
    [DllImport("user32.dll")] static extern int GetWindowText(nint hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    private delegate bool EnumWindowsProc(nint hWnd, nint lParam);
    private const int SW_RESTORE = 9;

    // ── 公开 API ─────────────────────────────────────────

    /// <summary>读取指定 HWND 的窗口标题。</summary>
    public static string GetWindowTitle(nint hwnd)
    {
#if WINDOWS
        if (hwnd == nint.Zero) return string.Empty;
        var sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
#else
        return string.Empty;
#endif
    }

    /// <summary>
    /// 通知到达时调用，在所有匹配进程的窗口中，找到最可能是"发出通知的那个"窗口的 HWND。
    /// 策略：在同名进程里，取标题与通知内容相关度最高的窗口；相关度相同则取最近前台窗口。
    /// </summary>
    public static nint CaptureTargetHwnd(string sourceApp, string notifTitle, string notifBody)
    {
        string[] candidates = ResolveProcessNames(sourceApp);
        var windows = EnumerateAppWindows(candidates);
        if (windows.Count == 0) return nint.Zero;

        // 用通知标题 + 正文在窗口标题里做关键词匹配，取最高得分
        nint best = nint.Zero;
        int bestScore = -1;

        foreach (var (hwnd, title) in windows)
        {
            int score = ScoreMatch(title, notifTitle, notifBody);
            if (score > bestScore)
            {
                bestScore = score;
                best = hwnd;
            }
        }

        return best;
    }

    /// <summary>
    /// 激活目标窗口。
    /// 优先级：① WindowTitle 精确匹配 → ② TargetHwnd → ③ 进程名随机
    /// </summary>
    public static bool Activate(nint targetHwnd, string sourceApp, string windowTitle = "")
    {
        // ① 用保存的 WindowTitle 在当前所有窗口里精确找
        if (!string.IsNullOrEmpty(windowTitle))
        {
            string[] candidates = ResolveProcessNames(sourceApp);
            var windows = EnumerateAppWindows(candidates);
            foreach (var (hwnd, title) in windows)
            {
                if (title.Equals(windowTitle, StringComparison.OrdinalIgnoreCase))
                    return BringToFront(hwnd);
            }
            // 标题可能因编辑状态变化（加了 • 前缀），做包含匹配再试一次
            foreach (var (hwnd, title) in windows)
            {
                if (title.Contains(windowTitle, StringComparison.OrdinalIgnoreCase) ||
                    windowTitle.Contains(title, StringComparison.OrdinalIgnoreCase))
                    return BringToFront(hwnd);
            }
        }

        // ② 降级：用捕获时的 HWND（句柄在多开时可能不准，但仍尝试）
        if (targetHwnd != nint.Zero && BringToFront(targetHwnd))
            return true;

        // ③ 最后回退：找同名进程的任意可见窗口
        string[] fallback = ResolveProcessNames(sourceApp);
        var fallbackWindows = EnumerateAppWindows(fallback);
        return fallbackWindows.Count > 0 && BringToFront(fallbackWindows[0].hwnd);
    }

    // ── 内部实现 ──────────────────────────────────────────

    private static string[] ResolveProcessNames(string sourceApp)
    {
        foreach (var kv in AppProcessMap)
            if (sourceApp.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return [sourceApp.ToLowerInvariant()];
    }

    private static List<(nint hwnd, string title)> EnumerateAppWindows(string[] processNames)
    {
        // 先收集目标进程 ID
        var pids = new HashSet<uint>();
        foreach (var name in processNames)
            foreach (var p in System.Diagnostics.Process.GetProcessesByName(name))
                pids.Add((uint)p.Id);

        var result = new List<(nint, string)>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd)) return true;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (!pids.Contains(pid)) return true;

            var sb = new StringBuilder(512);
            GetWindowText(hwnd, sb, sb.Capacity);
            string title = sb.ToString();
            if (string.IsNullOrWhiteSpace(title)) return true;

            result.Add((hwnd, title));
            return true;
        }, nint.Zero);

        return result;
    }

    /// <summary>
    /// 计算窗口标题与通知内容的相关度分数（越高越好）。
    /// 把通知标题/正文按空格/标点切词，在窗口标题里逐词命中计分。
    /// </summary>
    private static int ScoreMatch(string windowTitle, string notifTitle, string notifBody)
    {
        if (string.IsNullOrEmpty(windowTitle)) return 0;

        string lower = windowTitle.ToLowerInvariant();
        int score = 0;

        foreach (var token in Tokenize(notifTitle))
            if (lower.Contains(token)) score += 2;   // 标题关键词权重更高

        foreach (var token in Tokenize(notifBody))
            if (lower.Contains(token)) score += 1;

        return score;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        foreach (var word in text.ToLowerInvariant()
                     .Split([' ', '\t', '\n', '\r', '.', ',', ':', ';', '/', '\\', '(', ')'],
                            StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length >= 3)   // 过滤太短的词
                yield return word;
        }
    }

    private static bool BringToFront(nint hwnd)
    {
        if (hwnd == nint.Zero) return false;
        if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);
        return SetForegroundWindow(hwnd);
    }

#else
    public static nint CaptureTargetHwnd(string sourceApp, string notifTitle, string notifBody) => nint.Zero;
    public static bool Activate(nint targetHwnd, string sourceApp) => false;
    public static bool Activate(nint targetHwnd, string sourceApp, string windowTitle = "") => false;
#endif
}
